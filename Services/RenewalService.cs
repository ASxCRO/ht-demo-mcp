using System.Globalization;
using HtCustomerSupport.Repositories;

namespace HtCustomerSupport.Services;

public sealed record RenewalOffer(string OfferId, string Plan, decimal MonthlyEur, int CommitmentMonths, string? Device, decimal DeviceUpfrontEur, decimal DeviceMonthlyEur, decimal TotalCommitmentEur, int? DeviceDeliveryDaysAfterSigning, string Stage);
public sealed record RenewalRequest(string Id, string CustomerId, string ContractId, RenewalOffer Offer, string DeliveryAddress, string Status, string ContractDelivery, string DeviceDelivery, DateTimeOffset CreatedAt);

public sealed class RenewalService(ICustomerRepository repo)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, RenewalRequest> _requests = new(StringComparer.Ordinal);
    private static readonly RenewalOffer Standard = new("REN-STANDARD","Internet L",34.90m,24,null,0m,0m,837.60m,null,"standard");
    private static readonly RenewalOffer[] Retention = [
        new("REN-PS5","Internet L",39.90m,24,"PlayStation 5 Digital Edition",0m,0m,957.60m,5,"retention"),
        new("REN-PHONE","Internet L",34.90m,24,"Samsung Galaxy A56 5G",0m,0m,837.60m,5,"retention")];
    private (bool Eligible, int? Days, string Reason) Eligibility(string customerId,string contractId)
    {
        var customer=repo.Customers.SingleOrDefault(c=>c.Id==customerId) ?? throw new ArgumentException("Customer not found.");
        var contract=repo.Contracts.SingleOrDefault(c=>c.Id==contractId && c.CustomerId==customer.Id) ?? throw new ArgumentException("Contract not found for this account.");
        if(contract.Status!="Aktivan" || contract.TariffId!="TAR-002" || !repo.CustomerServices.Any(s=>s.CustomerId==customer.Id&&s.ContractId==contract.Id&&s.Status=="Aktivna"&&repo.Services.Any(x=>x.Id==s.ServiceId&&x.Category=="internet"))) return (false,null,"No eligible active Internet L contract.");
        if(!DateOnly.TryParseExact(contract.CommitmentEndDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var expiry)) return (false,null,"Commitment date needs review.");
        var days=expiry.DayNumber-DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        if(days<0||days>30) return (false,days,"Renewal campaign applies within 30 days before commitment expiry.");
        if(repo.Invoices.Any(i=>i.CustomerId==customer.Id&&i.OutstandingEur>0)) return (false,days,"Outstanding balance requires review.");
        return (true,days,"Eligible under internal campaign rules.");
    }
    public object Offers(string customerId,string? contractId,bool consideringSwitch)
    {
        if(!repo.Customers.Any(c=>c.Id==customerId)) throw new ArgumentException("Customer not found.");
        if(string.IsNullOrWhiteSpace(contractId))
        {
            var candidates=repo.Contracts.Where(c=>c.CustomerId==customerId && c.Status=="Aktivan" && c.TariffId=="TAR-002" && repo.CustomerServices.Any(x=>x.CustomerId==customerId&&x.ContractId==c.Id&&x.Status=="Aktivna"&&repo.Services.Any(y=>y.Id==x.ServiceId&&y.Category=="internet"))).ToArray();
            if(candidates.Length!=1) return new { Eligible=false, Reason="A unique active Internet L contract could not be selected; review the contracts.", Offers=Array.Empty<RenewalOffer>() };
            contractId=candidates[0].Id;
        }
        var e=Eligibility(customerId,contractId);
        var contract=repo.Contracts.Single(c=>c.Id==contractId&&c.CustomerId==customerId);
        if(!e.Eligible) return new { Eligible=false, ContractId=contractId, CommitmentEndDate=contract.CommitmentEndDate, DaysUntilExpiry=e.Days, Reason=e.Reason, Offers=Array.Empty<RenewalOffer>() };
        lock(_gate)
        {
            if(_requests.TryGetValue(customerId+"|"+contractId,out var existing)) return new { Eligible=true, ContractId=contractId, CommitmentEndDate=contract.CommitmentEndDate, DaysUntilExpiry=e.Days, Reason="Existing renewal request; do not create another.", ExistingRequest=existing, Offers=Array.Empty<RenewalOffer>() };
            return new { Eligible=true, ContractId=contractId, CommitmentEndDate=contract.CommitmentEndDate, DaysUntilExpiry=e.Days, Reason=e.Reason, Offers=consideringSwitch?Retention:new[]{Standard} };
        }
    }
    public RenewalRequest Create(string customerId,string contractId,string offerId,bool consideringSwitch,bool confirmed)
    {
        if(!confirmed) throw new ArgumentException("Explicit acceptance of the selected terms is required.");
        var key=customerId+"|"+contractId;
        lock(_gate)
        {
            if(_requests.TryGetValue(key,out var existing))
            {
                if(existing.Offer.OfferId!=offerId) throw new InvalidOperationException("A different renewal request already exists.");
                return existing;
            }
            var e=Eligibility(customerId,contractId);
            if(!e.Eligible) throw new InvalidOperationException(e.Reason);
            var offer=(consideringSwitch?Retention:new[]{Standard}).SingleOrDefault(o=>o.OfferId==offerId) ?? throw new ArgumentException("Offer not available under the selected campaign.");
            if(_requests.Count>=1000) throw new InvalidOperationException("Renewal request capacity reached.");
            var address=repo.Customers.Single(c=>c.Id==customerId).Address;
            var request=new RenewalRequest("RNW-"+Guid.NewGuid().ToString("N"),customerId,contractId,offer,address,"pending_contract_signature","Paper contract delivery requested; no physical dispatch performed.",offer.Device is null?"No device":"Delivery planned within five calendar days after signing, subject to fulfilment; not shipped.",DateTimeOffset.UtcNow);
            _requests.Add(key,request);
            return request;
        }
    }
}

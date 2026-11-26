using System.Globalization;
using System.Text;
using System.Text.Json;
using HtCustomerSupport.Repositories;

namespace HtCustomerSupport.Services;

public sealed record Coverage(string Address, string Status, string? Technology, int? MaxDownloadMbps, string Reason);
public sealed record RelocationEligibility(string Status, string? ContractId, string? CommitmentEndDate, string? Tariff, decimal? MonthlyEur, Coverage Coverage, string[] Conditions, string Reason);
public sealed record RelocationQuote(string Status, decimal? AmountEur, string Currency, string Reason);
public sealed record InstallationSlot(string SlotId, string Date, string Window, string TimeZone);
public sealed record RelocationRequest(string Id, string CustomerId, string Address, string SlotId, decimal FeeEur, string Status, DateTimeOffset CreatedAt);
public sealed record CallbackRequest(string Id, string CustomerId, string Destination, string Reason, string Status, string PreferredContactWindow, DateTimeOffset CreatedAt);

public sealed class RelocationService(ICustomerRepository repo)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Coverage[] _coverage = JsonSerializer.Deserialize<Coverage[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Data","coverage.json")),Json) ?? throw new InvalidOperationException("Missing coverage records.");
    private readonly object _gate = new();
    private readonly Dictionary<string,RelocationRequest> _orders = new(StringComparer.Ordinal);
    private readonly Dictionary<string,CallbackRequest> _callbacks = new(StringComparer.Ordinal);
    private static readonly string[] Conditions = ["Technical availability at the new address", "Existing contract terms and applicable relocation charge", "Agreement of an installation window"];
    private static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c=>char.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))).ToUpperInvariant();
    private static string Required(string value, string field, int max=250) => string.IsNullOrWhiteSpace(value)||value.Length>max ? throw new ArgumentException($"Provide {field} (1–{max} characters).") : value.Trim();
    private string Customer(string customerId) => repo.Customers.FirstOrDefault(c=>c.Id==customerId.Trim())?.Id ?? throw new ArgumentException("Customer not found.");
    public Coverage Availability(string address)
    {
        address=Required(address,"destination address");
        return _coverage.SingleOrDefault(x=>Normalize(x.Address)==Normalize(address)) ?? new Coverage(address,"review_required",null,null,"Address is not in the internal coverage inventory; specialist review is required.");
    }
    public RelocationEligibility Eligibility(string customerId,string address)
    {
        customerId=Customer(customerId);
        var cov=Availability(address);
        var internet=repo.CustomerServices.Where(s=>s.CustomerId==customerId && s.Status=="Aktivna" && repo.Services.Any(x=>x.Id==s.ServiceId&&x.Category=="internet")).ToArray();
        var matching=repo.Contracts.Where(c=>c.CustomerId==customerId && c.Status=="Aktivan" && internet.Any(s=>s.ContractId==c.Id)).ToArray();
        if(matching.Length!=1) return new("review_required",null,null,null,null,cov,Conditions,"A single active home internet contract could not be established.");
        var contract=matching[0];
        var tariff=repo.Tariffs.Single(t=>t.Id==contract.TariffId);
        if(!DateOnly.TryParseExact(contract.CommitmentEndDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _) || !tariff.Name.Contains("Internet",StringComparison.OrdinalIgnoreCase))
            return new("review_required",contract.Id,contract.CommitmentEndDate,tariff.Name,tariff.MonthlyEur,cov,Conditions,"Contract or tariff details require review.");
        var status=cov.Status=="available"?"eligible":cov.Status=="unavailable"?"not_eligible":"review_required";
        return new(status,contract.Id,contract.CommitmentEndDate,tariff.Name,tariff.MonthlyEur,cov,Conditions,cov.Reason);
    }
    public RelocationQuote Quote(string customerId,string address)
    {
        var e=Eligibility(customerId,address);
        return e.Status=="eligible" ? new("confirmed",29.90m,"EUR","Standard one-time relocation charge in the internal pricing rules; existing monthly price unchanged.")
            : new("review_required",null,"EUR","A fee cannot be confirmed until eligibility and technical availability are established.");
    }
    private InstallationSlot[] CandidateSlots(string address,string? preferredDate)
    {
        var today=DateOnly.FromDateTime(DateTime.UtcNow);
        var earliest=today.AddDays(5);
        if(!string.IsNullOrWhiteSpace(preferredDate))
        {
            if(!DateOnly.TryParseExact(preferredDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var requested)) throw new ArgumentException("Preferred date must be YYYY-MM-DD.");
            if(requested>earliest) earliest=requested;
        }
        if(earliest>today.AddDays(60)) return [];
        var result=new List<InstallationSlot>();
        for(var day=earliest;day<=today.AddDays(60)&&result.Count<6;day=day.AddDays(1))
        {
            if(day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            foreach(var (key,window) in new[]{("AM","09:00–12:00"),("PM","13:00–16:00")})
            {
                var date=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
                result.Add(new($"{date}-{key}",date,window,"Europe/Zagreb"));
            }
        }
        return result.ToArray();
    }
    public object Slots(string customerId,string address,string? preferredDate)
    {
        var e=Eligibility(customerId,address);
        if(e.Status!="eligible") return new { Status=e.Status, Slots=Array.Empty<InstallationSlot>(), Reason=e.Reason };
        lock(_gate) return new { Status="available", Slots=CandidateSlots(address,preferredDate).Where(s=>!_orders.Values.Any(o=>o.SlotId==s.SlotId)).ToArray(), Reason="Internal installation inventory only." };
    }
    public RelocationRequest Create(string customerId,string address,string slotId,bool confirmed)
    {
        if(!confirmed) throw new ArgumentException("Explicit customer confirmation is required.");
        customerId=Customer(customerId); address=Required(address,"destination address"); slotId=Required(slotId,"slot identifier",40);
        var e=Eligibility(customerId,address); var q=Quote(customerId,address);
        if(e.Status!="eligible"||q.AmountEur is null) throw new InvalidOperationException("Relocation must pass eligibility and pricing checks.");
        lock(_gate)
        {
            var key=customerId+"|"+Normalize(address)+"|"+slotId;
            if(_orders.TryGetValue(key,out var existing)) return existing;
            if(slotId.Length<10 || !CandidateSlots(address,slotId[..10]).Any(s=>s.SlotId==slotId)) throw new ArgumentException("Select an available installation slot from the inventory.");
            if(_orders.Values.Any(o=>o.SlotId==slotId)) throw new InvalidOperationException("Installation slot is no longer available.");
            if(_orders.Count>=1000) throw new InvalidOperationException("Relocation capacity reached.");
            var order=new RelocationRequest("REL-"+Guid.NewGuid().ToString("N"),customerId,e.Coverage.Address,slotId,q.AmountEur.Value,"recorded",DateTimeOffset.UtcNow);
            _orders.Add(key,order); return order;
        }
    }
    public CallbackRequest Callback(string customerId,string destination,string reason,bool confirmed,string? preferredContactWindow)
    {
        if(!confirmed) throw new ArgumentException("Explicit customer confirmation is required.");
        customerId=Customer(customerId); destination=Required(destination,"destination"); reason=Required(reason,"review reason",500);
        var window=string.IsNullOrWhiteSpace(preferredContactWindow)?"Not specified":Required(preferredContactWindow,"contact preference",100);
        lock(_gate)
        {
            var key=customerId+"|"+Normalize(destination);
            if(_callbacks.TryGetValue(key,out var existing)) return existing;
            if(_callbacks.Count>=1000) throw new InvalidOperationException("Callback request capacity reached.");
            var request=new CallbackRequest("CB-"+Guid.NewGuid().ToString("N"),customerId,destination,reason,"pending_customer_service_review",window,DateTimeOffset.UtcNow);
            _callbacks.Add(key,request); return request;
        }
    }
}

using System.ComponentModel;
using HtCustomerSupport.Repositories;
using HtCustomerSupport.Services;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class SupportTools
{
    private static readonly InMemoryCustomerRepository Repository = new();
    private static readonly CustomerSupportService Support = new(Repository);
    private static readonly RelocationService Relocation = new(Repository);
    private static readonly RenewalService Renewal = new(Repository);
    public static void Initialize() { _ = Repository.Customers.Count; _ = Relocation; _ = Renewal; }

    [McpServerTool(Name = "search_customers"), Description("Pretraži zapise interne evidencije prema korisničkoj oznaci, imenu, prezimenu, adresi, broju računa ili ugovora. Evidencija sadrži sintetičke podatke; ne unositi stvarne osobne podatke.")]
    public static object SearchCustomers(string? customerId = null, string? firstName = null, string? lastName = null, string? address = null, string? invoiceNumber = null, string? contractNumber = null) => Support.Search(customerId, firstName, lastName, address, invoiceNumber, contractNumber);
    [McpServerTool(Name = "get_customer_contracts"), Description("Dohvati ugovore odabranog korisnika iz interne evidencije.")]
    public static object GetContracts(string customerId) => Support.Contracts(customerId);
    [McpServerTool(Name = "get_invoices"), Description("Retrieve invoices, outstanding balance, recorded payment receipt/posting dates and reminder status when available from the internal ledger. Not live bank verification.")]
    public static object GetInvoices(string customerId) => Support.Invoices(customerId);
    [McpServerTool(Name = "get_tariffs"), Description("Dohvati tarife i cijene iz internog kataloga; nisu službene ponude operatera.")]
    public static object GetTariffs() => Support.Tariffs();
    [McpServerTool(Name = "get_services"), Description("Dohvati aktivne usluge i njihove statuse.")]
    public static object GetServices(string customerId) => Support.Services(customerId);
    [McpServerTool(Name = "list_service_catalog"), Description("Dohvati interni katalog usluga.")]
    public static object ListServiceCatalog() => Support.ServiceCatalog();
    [McpServerTool(Name = "get_troubleshooting_steps"), Description("Vrati jednostavne laičke korake za poteškoću s uslugom; opcionalno navedi simptom.")]
    public static object Troubleshoot(string service, string? symptom = null) => Support.Troubleshoot(service, symptom);
    [McpServerTool(Name = "check_service_outage"), Description("Provjeri stanje prema internoj evidenciji područja; nije provjera stvarne mreže.")]
    public static object CheckOutage(string area) => Support.Outage(area);
    [McpServerTool(Name = "find_available_employees"), Description("Dohvati dostupne zaposlenike iz interne evidencije po kategoriji i području, bez stvarnog transfera poziva.")]
    public static object FindEmployees(string? category = null, string? area = null) => Support.Employees(category, area);
    [McpServerTool(Name = "create_support_ticket"), Description("Nakon potvrde korisnika kreiraj prijavu i e-mail outbox zapis u memoriji. Nikakva stvarna poruka se ne šalje.")]
    public static object CreateTicket(string customerId, string summary, bool confirmed, string? category = null, string? employeeId = null) => Support.CreateTicket(customerId, summary, confirmed, category, employeeId);
    [McpServerTool(Name = "get_support_tickets"), Description("Dohvati prijave odabranog korisnika iz interne evidencije.")]
    public static object GetTickets(string customerId) => Support.Tickets(customerId);

    [McpServerTool(Name = "check_address_availability"), Description("Look up the exact supplied synthetic destination in internal coverage inventory. Unknown or survey-required address returns review_required; not live network coverage.")]
    public static object CheckAvailability(string address) => Relocation.Availability(address);
    [McpServerTool(Name = "check_relocation_eligibility"), Description("After account selection, check active home internet contract, commitment end date, tariff and address eligibility. Returns eligible, not_eligible or review_required.")]
    public static object CheckEligibility(string customerId, string address) => Relocation.Eligibility(customerId,address);
    [McpServerTool(Name = "calculate_relocation_cost"), Description("Calculate one-time relocation fee from internal rules. No amount returned until eligibility is confirmed.")]
    public static object CalculateRelocationCost(string customerId, string address) => Relocation.Quote(customerId,address);
    [McpServerTool(Name = "get_installation_slots"), Description("List internal installation windows for an eligible destination. Optional preferred date YYYY-MM-DD. No actual appointment is booked.")]
    public static object GetInstallationSlots(string customerId, string address, string? preferredDate = null) => Relocation.Slots(customerId,address,preferredDate);
    [McpServerTool(Name = "create_relocation_request"), Description("Record an internal relocation request for an eligible address, quoted fee and selected installation slot. Requires explicit confirmation of the move. Not an actual carrier order.")]
    public static object CreateRelocationRequest(string customerId, string address, string slotId, bool confirmed) => Relocation.Create(customerId,address,slotId,confirmed);
    [McpServerTool(Name = "schedule_customer_callback"), Description("Record a pending customer service review and callback request after explicit consent. No actual call is placed or guaranteed. Optional contact time preference, never collect real phone numbers.")]
    public static object ScheduleCustomerCallback(string customerId, string destination, string reason, bool confirmed, string? preferredContactWindow = null) => Relocation.Callback(customerId,destination,reason,confirmed,preferredContactWindow);

    [McpServerTool(Name = "get_renewal_offers"), Description("After exact account selection, resolve a unique active Internet L contract if contractId is omitted, return its expiry, eligibility and internal campaign offers. ExistingRequest means already recorded; do not create another. consideringSwitch=true only if the caller explicitly indicates they may switch provider. Prices include full commitment terms; not official carrier offers.")]
    public static object GetRenewalOffers(string customerId, string? contractId = null, bool consideringSwitch = false) => Renewal.Offers(customerId,contractId,consideringSwitch);
    [McpServerTool(Name = "create_renewal_request"), Description("Record an internal renewal request pending paper contract signature, only after the customer explicitly accepts the selected offer, price, commitment and delivery address. No actual contract dispatch, shipping or service change.")]
    public static object CreateRenewalRequest(string customerId, string contractId, string offerId, bool consideringSwitch, bool confirmed) => Renewal.Create(customerId,contractId,offerId,consideringSwitch,confirmed);
}

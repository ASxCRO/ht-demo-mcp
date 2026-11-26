using System.ComponentModel;
using HtCustomerSupport.Repositories;
using HtCustomerSupport.Services;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class SupportTools
{
    private static readonly InMemoryCustomerRepository Repository = new();
    private static readonly CustomerSupportService Support = new(Repository);
    public static void Initialize() => _ = Repository.Customers.Count;

    [McpServerTool(Name = "search_customers"), Description("Pretraži zapise interne evidencije prema korisničkoj oznaci, imenu, prezimenu, adresi, broju računa ili ugovora. Evidencija sadrži sintetičke podatke; ne unositi stvarne osobne podatke.")]
    public static object SearchCustomers(string? customerId = null, string? firstName = null, string? lastName = null, string? address = null, string? invoiceNumber = null, string? contractNumber = null) => Support.Search(customerId, firstName, lastName, address, invoiceNumber, contractNumber);
    [McpServerTool(Name = "get_customer_contracts"), Description("Dohvati ugovore odabranog korisnika iz interne evidencije.")]
    public static object GetContracts(string customerId) => Support.Contracts(customerId);
    [McpServerTool(Name = "get_invoices"), Description("Dohvati račune, iznose i evidentirani status plaćanja korisnika.")]
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
}

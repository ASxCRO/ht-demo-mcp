using System.ComponentModel;
using HtDemoMcp.Repositories;
using HtDemoMcp.Services;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class SupportTools
{
    private static readonly InMemoryDemoRepository Repository = new();
    private static readonly DemoSupportService Support = new(Repository);
    public static void Initialize() => _ = Repository.Customers.Count;

    [McpServerTool(Name = "search_customers"), Description("Pretraži isključivo sintetičke demo zapise prema ID-u, demo imenu, prezimenu, adresi, broju računa ili ugovora. Ne unositi stvarne osobne podatke.")]
    public static object SearchCustomers(string? customerId = null, string? firstName = null, string? lastName = null, string? address = null, string? invoiceNumber = null, string? contractNumber = null) => Support.Search(customerId, firstName, lastName, address, invoiceNumber, contractNumber);
    [McpServerTool(Name = "get_customer_contracts"), Description("Vrati fiktivne ugovore demo korisnika.")]
    public static object GetContracts(string customerId) => Support.Contracts(customerId);
    [McpServerTool(Name = "get_invoices"), Description("Vrati simulirane račune, iznose i status plaćanja demo korisnika.")]
    public static object GetInvoices(string customerId) => Support.Invoices(customerId);
    [McpServerTool(Name = "get_tariffs"), Description("Prikaži izmišljene tarife i cijene; nisu službene ponude HT-a.")]
    public static object GetTariffs() => Support.Tariffs();
    [McpServerTool(Name = "get_services"), Description("Prikaži simulirane aktivne usluge demo korisnika i njihove statuse.")]
    public static object GetServices(string customerId) => Support.Services(customerId);
    [McpServerTool(Name = "list_service_catalog"), Description("Prikaži javni sintetički katalog demo usluga.")]
    public static object ListServiceCatalog() => Support.ServiceCatalog();
    [McpServerTool(Name = "get_troubleshooting_steps"), Description("Vrati jednostavne laičke korake za demo poteškoću s uslugom; opcionalno navedi simptom.")]
    public static object Troubleshoot(string service, string? symptom = null) => Support.Troubleshoot(service, symptom);
    [McpServerTool(Name = "check_service_outage"), Description("Provjeri isključivo simulirano stanje na demo području; nije stvarni status mreže.")]
    public static object CheckOutage(string area) => Support.Outage(area);
    [McpServerTool(Name = "find_available_employees"), Description("Dohvati dostupne fiktivne zaposlenike po kategoriji i području, bez stvarnog transfera poziva.")]
    public static object FindEmployees(string? category = null, string? area = null) => Support.Employees(category, area);
    [McpServerTool(Name = "create_support_ticket"), Description("Nakon potvrde korisnika kreiraj demo prijavu i simulirani e-mail outbox zapis. Nikakva stvarna poruka se ne šalje.")]
    public static object CreateTicket(string customerId, string summary, bool confirmed, string? category = null, string? employeeId = null) => Support.CreateTicket(customerId, summary, confirmed, category, employeeId);
    [McpServerTool(Name = "get_support_tickets"), Description("Vrati postojeće simulirane prijave odabranog demo korisnika.")]
    public static object GetTickets(string customerId) => Support.Tickets(customerId);
}

using System.Collections.Concurrent;
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class SupportTools
{
    private record Customer(string Id, string Name, string Tariff, string Service, decimal Balance);
    private static readonly Customer[] Customers = [new("DEMO-001", "Demo korisnik A", "Mobilna M", "Mobilna telefonija", 24.90m), new("DEMO-002", "Demo korisnik B", "Internet L", "Optički internet", 0m)];
    private static readonly ConcurrentDictionary<string, object> Tickets = new();
    private static Customer Find(string customerId) => Customers.FirstOrDefault(c => c.Id.Equals(customerId, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Demo korisnik nije pronađen.");
    [McpServerTool(Name = "search_customers"), Description("Pronađi fiktivnog korisnika po demo identifikatoru. Ne koristi stvarne osobne podatke.")]
    public static object SearchCustomers(string customerId) => Customers.Where(c => c.Id.Equals(customerId, StringComparison.OrdinalIgnoreCase)).Select(c => new { c.Id, c.Name }).ToArray();
    [McpServerTool(Name = "get_customer_contracts"), Description("Vrati fiktivne ugovore demo korisnika.")]
    public static object GetContracts(string customerId) { var c = Find(customerId); return new { c.Id, c.Tariff, Status = "Aktivan", Demo = true }; }
    [McpServerTool(Name = "get_invoices"), Description("Vrati simulirani iznos nepodmirenih računa.")]
    public static object GetInvoices(string customerId) { var c = Find(customerId); return new { c.Id, OutstandingEur = c.Balance, Currency = "EUR", Demo = true }; }
    [McpServerTool(Name = "get_tariffs"), Description("Prikaži izmišljene tarife i cijene; nisu službene ponude HT-a.")]
    public static object GetTariffs() => new[] { new { Name = "Mobilna M", MonthlyEur = 24.90m }, new { Name = "Internet L", MonthlyEur = 34.90m } };
    [McpServerTool(Name = "get_services"), Description("Prikaži simulirane aktivne usluge demo korisnika.")]
    public static object GetServices(string customerId) { var c = Find(customerId); return new { c.Id, c.Service, Status = "Aktivna" }; }
    [McpServerTool(Name = "get_troubleshooting_steps"), Description("Vrati osnovne upute za internet ili mobilnu uslugu.")]
    public static string[] Troubleshoot(string service) => service.Contains("internet", StringComparison.OrdinalIgnoreCase) ? ["Provjeri napajanje i kabele.", "Ponovno pokreni usmjerivač.", "Testiraj vezu na drugom uređaju.", "Ako problem ostane, ponudi tehničku podršku."] : ["Provjeri način rada u zrakoplovu i signal.", "Ponovno pokreni uređaj.", "Ako problem ostane, ponudi tehničku podršku."];
    [McpServerTool(Name = "check_service_outage"), Description("Simulacija provjere izvanrednog stanja; ne daje stvarni status mreže.")]
    public static object CheckOutage(string area) => new { Area = area, Outage = false, Demo = true, Note = "Simulirani rezultat; stvarni status nije provjeren." };
    [McpServerTool(Name = "find_available_employees"), Description("Vrati fiktivni tim za eskalaciju, bez stvarnog transfera poziva.")]
    public static object FindEmployees() => new { Team = "Demo tehnička podrška", Available = true, TransferImplemented = false };
    [McpServerTool(Name = "create_support_ticket"), Description("Nakon potvrde korisnika zapiši demo prijavu u privremenu memoriju. Ne unositi stvarne osobne podatke.")]
    public static object CreateTicket(string customerId, string summary, bool confirmed) {
        Find(customerId);
        if (!confirmed) throw new ArgumentException("Potrebna je potvrda korisnika.");
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 500) throw new ArgumentException("Sažetak mora imati 1–500 znakova.");
        if (Tickets.Count >= 1000) throw new InvalidOperationException("Demo limit prijava je dosegnut.");
        var id = $"DEMO-{Guid.NewGuid():N}";
        var ticket = new { Id = id, CustomerId = customerId, Summary = summary, Status = "Otvoreno", Demo = true, CreatedAt = DateTimeOffset.UtcNow };
        Tickets.TryAdd(id, ticket);
        return ticket;
    }
}

using HtCustomerSupport.Models;
using HtCustomerSupport.Repositories;

namespace HtCustomerSupport.Services;

public sealed class CustomerSupportService(ICustomerRepository repo)
{
    private static bool Same(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static bool Match(string value, string? query) => string.IsNullOrWhiteSpace(query) || value.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    public Customer Find(string customerId) => repo.Customers.FirstOrDefault(c => Same(c.Id, customerId))
        ?? throw new ArgumentException("Korisnik nije pronađen.");
    public object Search(string? customerId, string? firstName, string? lastName, string? address, string? invoiceNumber, string? contractNumber)
    {
        if (new[] {customerId, firstName, lastName, address, invoiceNumber, contractNumber}.All(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Potreban je barem jedan identifikator iz interne evidencije.");
        return repo.Customers.Where(c =>
            (string.IsNullOrWhiteSpace(customerId) || Same(c.Id, customerId)) &&
            Match(c.FirstName, firstName) && Match(c.LastName, lastName) &&
            (string.IsNullOrWhiteSpace(address) || Match(c.Address + " " + c.City, address)) &&
            (string.IsNullOrWhiteSpace(invoiceNumber) || repo.Invoices.Any(i => i.CustomerId == c.Id && Same(i.Id, invoiceNumber))) &&
            (string.IsNullOrWhiteSpace(contractNumber) || repo.Contracts.Any(i => i.CustomerId == c.Id && Same(i.Id, contractNumber))))
            .Take(20).Select(c => new { c.Id, c.Name, c.FirstName, c.LastName, c.Address, c.City }).ToArray();
    }
    public object Contracts(string customerId)
    {
        var c = Find(customerId);
        var rows = repo.Contracts.Where(x => x.CustomerId == c.Id).Select(x => new { x.Id, x.Status, x.StartDate, x.CommitmentEndDate, Tariff = repo.Tariffs.Single(t => t.Id == x.TariffId).Name, MonthlyEur = repo.Tariffs.Single(t => t.Id == x.TariffId).MonthlyEur }).ToArray();
        return new { c.Id, Contracts = rows, Tariff = rows.FirstOrDefault()?.Tariff, Status = rows.FirstOrDefault()?.Status };
    }
    public object Invoices(string customerId)
    {
        var c = Find(customerId);
        var rows = repo.Invoices.Where(x => x.CustomerId == c.Id).OrderByDescending(x => x.Period).ToArray();
        return new { c.Id, OutstandingEur = rows.Sum(x => x.OutstandingEur), Currency = "EUR", Invoices = rows };
    }
    public object Tariffs() => repo.Tariffs.Select(x => new { x.Id, x.Name, x.MonthlyEur, x.Description }).ToArray();
    public object Services(string customerId)
    {
        var c = Find(customerId);
        var rows = repo.CustomerServices.Where(x => x.CustomerId == c.Id).Select(x => new { x.Id, x.ContractId, x.Status, Service = repo.Services.Single(s => s.Id == x.ServiceId).Name }).ToArray();
        return new { c.Id, Service = rows.FirstOrDefault()?.Service, Status = rows.FirstOrDefault()?.Status, Services = rows };
    }
    public object ServiceCatalog() => repo.Services;
    private static string Category(string? value)
    {
        var s = value?.ToLowerInvariant() ?? "";
        if (s.Contains("račun") || s.Contains("billing") || s.Contains("invoice")) return "billing";
        if (s.Contains("ugovor") || s.Contains("contract") || s.Contains("preseljen") || s.Contains("relocation")) return "contracts";
        if (s.Contains("sigurn") || s.Contains("security") || s.Contains("phishing")) return "security";
        if (s.Contains("prigovor") || s.Contains("complaint")) return "complaints";
        if (s.Contains("teren") || s.Contains("field")) return "field";
        if (s.Contains("tarif") || s.Contains("prodaj") || s.Contains("sales")) return "sales";
        if (s.Contains("tv") || s.Contains("televiz")) return "tv";
        if (s.Contains("mobil") || s.Contains("poziv") || s.Contains("sim") || s.Contains("roaming")) return "mobile";
        if (s.Contains("internet") || s.Contains("wi-fi") || s.Contains("wifi")) return "internet";
        return "general";
    }
    public object Troubleshoot(string service, string? symptom)
    {
        var category = Category(service);
        var rows = repo.Solutions.Where(s => s.Category == category).ToArray();
        if (!string.IsNullOrWhiteSpace(symptom))
        {
            var tokens = symptom.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(t => t.Length >= 3).ToArray();
            rows = rows.OrderByDescending(s => tokens.Count(t => s.Symptom.Contains(t, StringComparison.OrdinalIgnoreCase))).ToArray();
        }
        return new { Service = service, Solutions = rows.Take(5).ToArray() };
    }
    public object Outage(string area)
    {
        var match = repo.Outages.FirstOrDefault(o => Same(o.Area, area));
        return new { Area = area, Outage = (bool?)match?.Outage, KnownArea = match is not null, Service = match?.Service, Note = match?.Note ?? "Nema podatka za to područje; stvarni status nije provjeren." };
    }
    private IEnumerable<Employee> Candidates(string? category, string? area) => repo.Employees.Where(e => e.Available &&
        (string.IsNullOrWhiteSpace(category) || e.Team == Category(category)) &&
        (string.IsNullOrWhiteSpace(area) || Same(e.City, area) || e.City == "Sve regije"))
        .OrderByDescending(e => Same(e.City, area));
    public object Employees(string? category, string? area)
    {
        var rows = Candidates(category, area).ToArray();
        return new { Team = rows.FirstOrDefault()?.Team, Available = rows.Length > 0, Employees = rows.Select(e => new { e.Id, e.Name, e.Team, e.City }).ToArray(), TransferImplemented = false };
    }
    public object CreateTicket(string customerId, string summary, bool confirmed, string? category, string? employeeId)
    {
        var c = Find(customerId);
        if (!confirmed) throw new ArgumentException("Potrebna je potvrda korisnika.");
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 500) throw new ArgumentException("Sažetak mora imati 1–500 znakova.");
        var team = Category(category ?? summary);
        var candidates = Candidates(team, c.City);
        var employee = string.IsNullOrWhiteSpace(employeeId) ? candidates.FirstOrDefault() : candidates.FirstOrDefault(e => Same(e.Id, employeeId));
        if (employee is null) throw new InvalidOperationException("Nema dostupnog odgovarajućeg zaposlenika u internoj evidenciji.");
        var now = DateTimeOffset.UtcNow;
        var ticket = new Ticket("TKT-" + Guid.NewGuid().ToString("N"), c.Id, summary.Trim(), team, employee.Id, "Otvoreno", now);
        var message = new OutboxMessage("MAIL-" + Guid.NewGuid().ToString("N"), ticket.Id, employee.Email, "Prijava " + ticket.Id, ticket.Summary, "Na čekanju - nije poslano", now);
        repo.AddTicket(ticket, message);
        return new { ticket.Id, ticket.CustomerId, ticket.Summary, ticket.Status, ticket.Category, ticket.EmployeeId, ticket.CreatedAt, OutboxId = message.Id, EmailSent = false };
    }
    public object Tickets(string customerId)
    {
        var c = Find(customerId);
        return new { c.Id, Tickets = repo.GetTickets().Where(t => t.CustomerId == c.Id).OrderByDescending(t => t.CreatedAt).ToArray() };
    }
}

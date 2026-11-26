using System.Collections.Concurrent;
using System.Text.Json;
using HtCustomerSupport.Models;

namespace HtCustomerSupport.Repositories;

public interface ICustomerRepository
{
    IReadOnlyList<Customer> Customers { get; }
    IReadOnlyList<Contract> Contracts { get; }
    IReadOnlyList<Invoice> Invoices { get; }
    IReadOnlyList<Tariff> Tariffs { get; }
    IReadOnlyList<Service> Services { get; }
    IReadOnlyList<CustomerService> CustomerServices { get; }
    IReadOnlyList<Employee> Employees { get; }
    IReadOnlyList<Solution> Solutions { get; }
    IReadOnlyList<OutageRecord> Outages { get; }
    IReadOnlyList<Ticket> GetTickets();
    IReadOnlyList<OutboxMessage> GetOutbox();
    void AddTicket(Ticket ticket, OutboxMessage message);
}

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, Ticket> _tickets = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, OutboxMessage> _outbox = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private static T[] Load<T>(string name) =>
        JsonSerializer.Deserialize<T[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", name + ".json")), Options)
        ?? throw new InvalidOperationException("Invalid customer seed: " + name);
    public IReadOnlyList<Customer> Customers { get; } = Load<Customer>("customers");
    public IReadOnlyList<Contract> Contracts { get; } = Load<Contract>("contracts");
    public IReadOnlyList<Invoice> Invoices { get; } = Load<Invoice>("invoices");
    public IReadOnlyList<Tariff> Tariffs { get; } = Load<Tariff>("tariffs");
    public IReadOnlyList<Service> Services { get; } = Load<Service>("services");
    public IReadOnlyList<CustomerService> CustomerServices { get; } = Load<CustomerService>("customer-services");
    public IReadOnlyList<Employee> Employees { get; } = Load<Employee>("employees");
    public IReadOnlyList<Solution> Solutions { get; } = Load<Solution>("troubleshooting");
    public IReadOnlyList<OutageRecord> Outages { get; } = Load<OutageRecord>("outages");
    public InMemoryCustomerRepository()
    {
        foreach (var ticket in Load<Ticket>("tickets")) _tickets.TryAdd(ticket.Id, ticket);
        if (Contracts.Any(c => !Customers.Any(x => x.Id == c.CustomerId) || !Tariffs.Any(x => x.Id == c.TariffId)) ||
            Invoices.Any(i => !Contracts.Any(c => c.Id == i.ContractId && c.CustomerId == i.CustomerId)) ||
            CustomerServices.Any(s => !Contracts.Any(c => c.Id == s.ContractId && c.CustomerId == s.CustomerId) || !Services.Any(x => x.Id == s.ServiceId)) ||
            _tickets.Values.Any(t => !Customers.Any(c => c.Id == t.CustomerId) || !Employees.Any(e => e.Id == t.EmployeeId)))
            throw new InvalidOperationException("Customer seed contains invalid relationships.");
    }
    public IReadOnlyList<Ticket> GetTickets() { lock (_gate) return _tickets.Values.ToArray(); }
    public IReadOnlyList<OutboxMessage> GetOutbox() { lock (_gate) return _outbox.Values.ToArray(); }
    public void AddTicket(Ticket ticket, OutboxMessage message)
    {
        lock (_gate)
        {
            if (_tickets.Count >= 1000) throw new InvalidOperationException("Limit prijava je dosegnut.");
            if (!_tickets.TryAdd(ticket.Id, ticket)) throw new InvalidOperationException("Prijava već postoji.");
            if (!_outbox.TryAdd(message.Id, message))
            {
                _tickets.TryRemove(ticket.Id, out _);
                throw new InvalidOperationException("Outbox zapis već postoji.");
            }
        }
    }
}

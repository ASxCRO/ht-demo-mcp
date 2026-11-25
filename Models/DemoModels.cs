namespace HtDemoMcp.Models;

public sealed record Customer(string Id, string FirstName, string LastName, string Name, string Address, string City, string Email);
public sealed record Contract(string Id, string CustomerId, string TariffId, string Status, string StartDate);
public sealed record Invoice(string Id, string CustomerId, string ContractId, string Period, string DueDate, decimal AmountEur, string Status, decimal OutstandingEur);
public sealed record Tariff(string Id, string Name, decimal MonthlyEur, string Description);
public sealed record Service(string Id, string Name, string Category);
public sealed record CustomerService(string Id, string CustomerId, string ContractId, string ServiceId, string Status);
public sealed record Employee(string Id, string Name, string Team, string City, bool Available, string Email);
public sealed record Solution(string Id, string Category, string Symptom, string Question, string[] Steps);
public sealed record OutageRecord(string Id, string Area, bool Outage, string Service, string Note);
public sealed record Ticket(string Id, string CustomerId, string Summary, string Category, string EmployeeId, string Status, bool Demo, DateTimeOffset CreatedAt);
public sealed record OutboxMessage(string Id, string TicketId, string Recipient, string Subject, string Body, string Status, DateTimeOffset CreatedAt);

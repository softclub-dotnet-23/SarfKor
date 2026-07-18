using Domain.Common;

namespace Domain.Customers;

public class Customer : Entity
{
    public required string PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

using Domain.Common;

namespace Domain.Inventory;

public class Supplier : Entity
{
    public required string Name { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
}

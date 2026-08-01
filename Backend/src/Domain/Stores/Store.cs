using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Stores;

public class Store : Entity
{
    public required string OwnerUserId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required GeoLocation Location { get; set; }
    public StoreStatus Status { get; set; }
}

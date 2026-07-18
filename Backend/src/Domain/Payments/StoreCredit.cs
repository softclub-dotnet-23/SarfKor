using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Payments;

public class StoreCredit : Entity
{
    public int StoreId { get; set; }
    public int CustomerId { get; set; }
    public required Money Balance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

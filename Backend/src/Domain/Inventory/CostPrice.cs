using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Inventory;

public class CostPrice : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public required Money Amount { get; set; }
    public required string SetByUserId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
}

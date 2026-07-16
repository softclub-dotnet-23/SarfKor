using Domain.Common;

namespace Domain.Inventory;

public class StockMovement : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public StockMovementType Type { get; set; }
    public int QuantityDelta { get; set; }
    public string? Reason { get; set; }
    public int? RelatedSaleTransactionId { get; set; }
    public int? SupplierId { get; set; }
    public required string PerformedByUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

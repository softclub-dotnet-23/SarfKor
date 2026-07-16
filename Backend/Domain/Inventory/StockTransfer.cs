using Domain.Common;

namespace Domain.Inventory;

public class StockTransfer : Entity
{
    public int ProductId { get; set; }
    public int FromStoreId { get; set; }
    public int ToStoreId { get; set; }
    public int Quantity { get; set; }
    public required string InitiatedByUserId { get; set; }
    public StockTransferStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

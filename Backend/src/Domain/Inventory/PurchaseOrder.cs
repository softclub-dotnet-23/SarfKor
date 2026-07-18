using Domain.Common;

namespace Domain.Inventory;

public class PurchaseOrder : Entity
{
    public int StoreId { get; set; }
    public int SupplierId { get; set; }
    public required string CreatedByUserId { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public List<PurchaseOrderLineItem> Lines { get; set; } = [];
}

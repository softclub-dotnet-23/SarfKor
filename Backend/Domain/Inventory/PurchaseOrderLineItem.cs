using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Inventory;

public class PurchaseOrderLineItem : Entity
{
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public required Money UnitCost { get; set; }
}

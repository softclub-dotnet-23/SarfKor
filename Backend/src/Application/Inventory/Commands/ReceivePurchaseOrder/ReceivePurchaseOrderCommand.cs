namespace Application.Inventory.Commands.ReceivePurchaseOrder;

public sealed record ReceivePurchaseOrderCommand(int PurchaseOrderId, string PerformedByUserId);

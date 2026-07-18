namespace Application.Inventory.Commands.SubmitPurchaseOrder;

public sealed record SubmitPurchaseOrderCommand(int PurchaseOrderId, string PerformedByUserId);

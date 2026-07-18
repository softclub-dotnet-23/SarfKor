namespace Application.Inventory.Commands.CreatePurchaseOrder;

public sealed record CreatePurchaseOrderLineInput(int ProductId, int Quantity, decimal UnitCost, string Currency);

public sealed record CreatePurchaseOrderCommand(
    int StoreId,
    int SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines,
    string PerformedByUserId);

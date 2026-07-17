namespace Application.Inventory.Commands.RecordStockReceipt;

public sealed record RecordStockReceiptCommand(
    int StoreId,
    int ProductId,
    int Quantity,
    string PerformedByUserId,
    int? SupplierId);

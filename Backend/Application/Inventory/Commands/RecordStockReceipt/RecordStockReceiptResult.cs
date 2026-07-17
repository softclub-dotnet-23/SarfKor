namespace Application.Inventory.Commands.RecordStockReceipt;

public sealed record RecordStockReceiptResult(RecordStockReceiptOutcome Outcome, int? StockMovementId);

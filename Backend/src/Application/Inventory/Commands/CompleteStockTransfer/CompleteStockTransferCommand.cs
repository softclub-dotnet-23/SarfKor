namespace Application.Inventory.Commands.CompleteStockTransfer;

public sealed record CompleteStockTransferCommand(int StockTransferId, string PerformedByUserId);

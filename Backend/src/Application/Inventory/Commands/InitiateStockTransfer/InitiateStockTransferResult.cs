namespace Application.Inventory.Commands.InitiateStockTransfer;

public enum InitiateStockTransferOutcome
{
    Initiated,
    FromStoreNotFound,
    ToStoreNotFound,
    Forbidden,
    InsufficientStock
}

public sealed record InitiateStockTransferResult(InitiateStockTransferOutcome Outcome, int? StockTransferId);

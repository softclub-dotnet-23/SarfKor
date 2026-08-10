namespace Application.Inventory.Commands.InitiateStockTransfer;

public enum InitiateStockTransferOutcome
{
    Initiated,
    FromStoreNotFound,
    ToStoreNotFound,
    Forbidden,
    InsufficientStock,
    SubscriptionInactive
}

public sealed record InitiateStockTransferResult(InitiateStockTransferOutcome Outcome, int? StockTransferId);

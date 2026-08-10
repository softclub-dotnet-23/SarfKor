namespace Application.Inventory.Commands.CompleteStockTransfer;

public enum CompleteStockTransferOutcome
{
    Completed,
    NotFound,
    Forbidden,
    NotInTransit,
    SubscriptionInactive
}

public sealed record CompleteStockTransferResult(CompleteStockTransferOutcome Outcome);

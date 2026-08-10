namespace Application.Inventory.Commands.ReceivePurchaseOrder;

public enum ReceivePurchaseOrderOutcome
{
    Received,
    NotFound,
    Forbidden,
    NotSubmitted,
    SubscriptionInactive
}

public sealed record ReceivePurchaseOrderResult(ReceivePurchaseOrderOutcome Outcome);

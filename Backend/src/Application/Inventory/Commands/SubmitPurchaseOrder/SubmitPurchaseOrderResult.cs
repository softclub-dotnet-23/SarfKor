namespace Application.Inventory.Commands.SubmitPurchaseOrder;

public enum SubmitPurchaseOrderOutcome
{
    Submitted,
    NotFound,
    Forbidden,
    NotDraft,
    SubscriptionInactive
}

public sealed record SubmitPurchaseOrderResult(SubmitPurchaseOrderOutcome Outcome);

namespace Application.Inventory.Commands.SubmitPurchaseOrder;

public enum SubmitPurchaseOrderOutcome
{
    Submitted,
    NotFound,
    Forbidden,
    NotDraft
}

public sealed record SubmitPurchaseOrderResult(SubmitPurchaseOrderOutcome Outcome);

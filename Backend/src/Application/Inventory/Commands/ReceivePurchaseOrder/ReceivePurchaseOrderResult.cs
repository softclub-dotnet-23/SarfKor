namespace Application.Inventory.Commands.ReceivePurchaseOrder;

public enum ReceivePurchaseOrderOutcome
{
    Received,
    NotFound,
    Forbidden,
    NotSubmitted
}

public sealed record ReceivePurchaseOrderResult(ReceivePurchaseOrderOutcome Outcome);

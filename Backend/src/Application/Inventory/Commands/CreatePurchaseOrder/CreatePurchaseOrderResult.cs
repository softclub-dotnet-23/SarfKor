namespace Application.Inventory.Commands.CreatePurchaseOrder;

public enum CreatePurchaseOrderOutcome
{
    Created,
    StoreNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record CreatePurchaseOrderResult(CreatePurchaseOrderOutcome Outcome, int? PurchaseOrderId);

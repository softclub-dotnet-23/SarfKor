namespace Application.Inventory.Commands.CreateSupplier;

public enum CreateSupplierOutcome
{
    Created,
    StoreNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record CreateSupplierResult(CreateSupplierOutcome Outcome, int? SupplierId);

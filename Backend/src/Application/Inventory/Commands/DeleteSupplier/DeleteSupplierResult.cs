namespace Application.Inventory.Commands.DeleteSupplier;

public enum DeleteSupplierOutcome
{
    Deleted,
    NotFound,
    InUse,
    Forbidden,
    SubscriptionInactive
}

public sealed record DeleteSupplierResult(DeleteSupplierOutcome Outcome);

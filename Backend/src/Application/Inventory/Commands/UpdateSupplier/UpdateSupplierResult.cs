namespace Application.Inventory.Commands.UpdateSupplier;

public enum UpdateSupplierOutcome
{
    Updated,
    NotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record UpdateSupplierResult(UpdateSupplierOutcome Outcome);

namespace Application.Inventory.Commands.UpdateSupplier;

public enum UpdateSupplierOutcome
{
    Updated,
    NotFound,
    Forbidden
}

public sealed record UpdateSupplierResult(UpdateSupplierOutcome Outcome);

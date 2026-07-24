namespace Application.Inventory.Commands.UpdateSupplier;

public enum UpdateSupplierOutcome
{
    Updated,
    NotFound
}

public sealed record UpdateSupplierResult(UpdateSupplierOutcome Outcome);

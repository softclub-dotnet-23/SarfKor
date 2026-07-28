namespace Application.Inventory.Commands.DeleteSupplier;

public enum DeleteSupplierOutcome
{
    Deleted,
    NotFound,
    InUse
}

public sealed record DeleteSupplierResult(DeleteSupplierOutcome Outcome);

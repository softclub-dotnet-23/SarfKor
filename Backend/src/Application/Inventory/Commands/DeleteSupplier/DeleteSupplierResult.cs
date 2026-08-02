namespace Application.Inventory.Commands.DeleteSupplier;

public enum DeleteSupplierOutcome
{
    Deleted,
    NotFound,
    InUse,
    Forbidden
}

public sealed record DeleteSupplierResult(DeleteSupplierOutcome Outcome);

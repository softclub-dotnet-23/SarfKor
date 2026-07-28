namespace Application.Catalog.Commands.DeleteTaxRate;

public enum DeleteTaxRateOutcome
{
    Deleted,
    NotFound,
    InUse
}

public sealed record DeleteTaxRateResult(DeleteTaxRateOutcome Outcome);

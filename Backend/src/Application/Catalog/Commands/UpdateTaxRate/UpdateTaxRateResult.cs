namespace Application.Catalog.Commands.UpdateTaxRate;

public enum UpdateTaxRateOutcome
{
    Updated,
    NotFound,
    CategoryNotFound
}

public sealed record UpdateTaxRateResult(UpdateTaxRateOutcome Outcome);

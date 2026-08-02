namespace Application.Catalog.Commands.CreateTaxRate;

public enum CreateTaxRateOutcome
{
    Created,
    CategoryNotFound
}

public sealed record CreateTaxRateResult(CreateTaxRateOutcome Outcome, int? TaxRateId);

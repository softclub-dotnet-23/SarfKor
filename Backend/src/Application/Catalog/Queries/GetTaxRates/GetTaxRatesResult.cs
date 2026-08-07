namespace Application.Catalog.Queries.GetTaxRates;

public sealed record TaxRateDto(int TaxRateId, string Name, decimal Percentage, int? CategoryId, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);

public sealed record GetTaxRatesResult(IReadOnlyList<TaxRateDto> TaxRates);

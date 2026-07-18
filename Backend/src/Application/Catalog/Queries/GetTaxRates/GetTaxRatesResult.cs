namespace Application.Catalog.Queries.GetTaxRates;

public sealed record TaxRateDto(int TaxRateId, string Name, decimal Percentage, int? CategoryId);

public sealed record GetTaxRatesResult(IReadOnlyList<TaxRateDto> TaxRates);

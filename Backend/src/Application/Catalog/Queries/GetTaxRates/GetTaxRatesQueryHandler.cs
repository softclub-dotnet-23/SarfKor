using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Queries.GetTaxRates;

public sealed class GetTaxRatesQueryHandler(ITaxRateRepository taxRateRepository) : IQueryHandler<GetTaxRatesQuery, GetTaxRatesResult>
{
    public async Task<GetTaxRatesResult> Handle(GetTaxRatesQuery query, CancellationToken cancellationToken)
    {
        var taxRates = await taxRateRepository.GetAllAsync(cancellationToken);
        return new GetTaxRatesResult(taxRates.Select(t => new TaxRateDto(t.Id, t.Name, t.Percentage, t.CategoryId, t.EffectiveFrom, t.EffectiveTo)).ToList());
    }
}

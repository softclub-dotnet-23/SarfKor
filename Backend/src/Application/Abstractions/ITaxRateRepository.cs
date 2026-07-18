using Domain.Catalog;

namespace Application.Abstractions;

public interface ITaxRateRepository
{
    Task<IReadOnlyList<TaxRate>> GetAllAsync(CancellationToken cancellationToken);
    void Add(TaxRate taxRate);
}

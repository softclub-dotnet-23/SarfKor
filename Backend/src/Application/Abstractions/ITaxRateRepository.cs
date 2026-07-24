using Domain.Catalog;

namespace Application.Abstractions;

public interface ITaxRateRepository
{
    Task<IReadOnlyList<TaxRate>> GetAllAsync(CancellationToken cancellationToken);
    Task<TaxRate?> GetByIdAsync(int taxRateId, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(int taxRateId, CancellationToken cancellationToken);
    void Add(TaxRate taxRate);
    void Remove(TaxRate taxRate);
}

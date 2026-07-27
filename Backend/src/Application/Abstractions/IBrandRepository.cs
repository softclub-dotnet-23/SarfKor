using Domain.Catalog;

namespace Application.Abstractions;

public interface IBrandRepository
{
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken);
    Task<Brand?> GetByIdAsync(int brandId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int brandId, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(int brandId, CancellationToken cancellationToken);
    void Add(Brand brand);
    void Remove(Brand brand);
}

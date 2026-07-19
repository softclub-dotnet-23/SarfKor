using Domain.Catalog;

namespace Application.Abstractions;

public interface IProductBundleRepository
{
    Task<ProductBundle?> GetByIdAsync(int productBundleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductBundle>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(ProductBundle productBundle);
}

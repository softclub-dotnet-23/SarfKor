using Domain.Catalog;

namespace Application.Abstractions;

public interface IProductBundleRepository
{
    Task<IReadOnlyList<ProductBundle>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(ProductBundle productBundle);
}

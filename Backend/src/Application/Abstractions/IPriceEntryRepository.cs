using Domain.Pricing;

namespace Application.Abstractions;

public interface IPriceEntryRepository
{
    Task<IReadOnlyList<PriceEntry>> GetLatestPerStoreAsync(int productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceEntry>> GetLatestPerStoreForProductsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken);
    Task<PriceEntry?> GetLatestForStoreAsync(int productId, int storeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceEntry>> GetLatestForStoreForProductsAsync(int storeId, IReadOnlyCollection<int> productIds, CancellationToken cancellationToken);
    Task<PriceEntry?> GetByIdAsync(int priceEntryId, CancellationToken cancellationToken);
    void Add(PriceEntry priceEntry);
}

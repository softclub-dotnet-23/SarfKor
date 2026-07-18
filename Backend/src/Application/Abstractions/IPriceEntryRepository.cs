using Domain.Pricing;

namespace Application.Abstractions;

public interface IPriceEntryRepository
{
    Task<IReadOnlyList<PriceEntry>> GetLatestPerStoreAsync(int productId, CancellationToken cancellationToken);
    Task<PriceEntry?> GetLatestForStoreAsync(int productId, int storeId, CancellationToken cancellationToken);
    void Add(PriceEntry priceEntry);
}

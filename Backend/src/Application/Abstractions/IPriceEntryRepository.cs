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

    /// <summary>Total submissions by this user and how many are currently verified — the "число
    /// внесённых цен и сколько из них подтвердилось" line on the Admin user card (ADMIN_PROMPT.md
    /// §2.3).</summary>
    Task<(int Total, int Verified)> CountByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<int> CountInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

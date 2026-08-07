using Domain.Sales;

namespace Application.Abstractions;

public sealed record ProductSalesSummary(int ProductId, int TotalQuantity);

public interface ISaleTransactionRepository
{
    Task<SaleTransaction?> GetByIdempotencyKeyAsync(int storeId, string idempotencyKey, CancellationToken cancellationToken);
    Task<SaleTransaction?> GetByIdAsync(int saleTransactionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SaleTransaction>> GetCompletedInRangeAsync(int storeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    Task<IReadOnlyList<SaleTransaction>> GetAllInRangeAsync(int storeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSalesSummary>> GetTopSellingProductsAsync(int? storeId, int limit, CancellationToken cancellationToken);
    void Add(SaleTransaction saleTransaction);

    /// <summary>Null if this store has never recorded a sale — used both by store diagnostics
    /// (§2.6) and the "silent stores" platform metric (§2.5).</summary>
    Task<DateTimeOffset?> GetLastSaleAtAsync(int storeId, CancellationToken cancellationToken);

    /// <summary>Batched version of <see cref="GetLastSaleAtAsync"/> for the platform metrics page —
    /// avoids one query per store. A store missing from the result has never sold anything.</summary>
    Task<IReadOnlyDictionary<int, DateTimeOffset>> GetLastSaleAtByStoreIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);

    Task<int> CountAcrossPlatformInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    /// <summary>Bare timestamps only (no line items/joins) — the time-series chart's data source,
    /// grouped by day in the query handler rather than adding a per-granularity SQL variant.</summary>
    Task<IReadOnlyList<DateTimeOffset>> GetCreatedAtAcrossPlatformInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

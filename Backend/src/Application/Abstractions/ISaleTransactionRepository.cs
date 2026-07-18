using Domain.Sales;

namespace Application.Abstractions;

public sealed record ProductSalesSummary(int ProductId, int TotalQuantity);

public interface ISaleTransactionRepository
{
    Task<SaleTransaction?> GetByIdempotencyKeyAsync(int storeId, string idempotencyKey, CancellationToken cancellationToken);
    Task<SaleTransaction?> GetByIdAsync(int saleTransactionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SaleTransaction>> GetCompletedInRangeAsync(int storeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSalesSummary>> GetTopSellingProductsAsync(int? storeId, int limit, CancellationToken cancellationToken);
    void Add(SaleTransaction saleTransaction);
}

using Application.Abstractions;
using Domain.Sales;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SaleTransactionRepository(AppDbContext dbContext) : ISaleTransactionRepository
{
    public Task<SaleTransaction?> GetByIdempotencyKeyAsync(int storeId, string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.SaleTransactions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<SaleTransaction?> GetByIdAsync(int saleTransactionId, CancellationToken cancellationToken) =>
        dbContext.SaleTransactions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == saleTransactionId, cancellationToken);

    public async Task<IReadOnlyList<SaleTransaction>> GetCompletedInRangeAsync(int storeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        await dbContext.SaleTransactions
            .Include(s => s.Lines)
            .Where(s => s.StoreId == storeId
                && s.Status == SaleStatus.Completed
                && s.CreatedAt >= from
                && s.CreatedAt < to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SaleTransaction>> GetAllInRangeAsync(int storeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        await dbContext.SaleTransactions
            .Include(s => s.Lines)
            .Where(s => s.StoreId == storeId && s.CreatedAt >= from && s.CreatedAt < to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductSalesSummary>> GetTopSellingProductsAsync(int? storeId, int limit, CancellationToken cancellationToken)
    {
        var salesQuery = dbContext.SaleTransactions.Where(s => s.Status == SaleStatus.Completed);

        if (storeId.HasValue)
            salesQuery = salesQuery.Where(s => s.StoreId == storeId.Value);

        // Aggregated in SQL — previously materialized every matching SaleLineItem platform-wide
        // (unbounded when storeId is null) before grouping in memory.
        return await (
            from line in dbContext.SaleLineItems
            join sale in salesQuery on line.SaleTransactionId equals sale.Id
            group line by line.ProductId into g
            orderby g.Sum(x => x.Quantity) descending
            select new ProductSalesSummary(g.Key, g.Sum(x => x.Quantity))
        ).Take(limit).ToListAsync(cancellationToken);
    }

    public void Add(SaleTransaction saleTransaction) => dbContext.SaleTransactions.Add(saleTransaction);

    public Task<DateTimeOffset?> GetLastSaleAtAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.SaleTransactions
            .Where(s => s.StoreId == storeId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => (DateTimeOffset?)s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, DateTimeOffset>> GetLastSaleAtByStoreIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken)
    {
        var rows = await dbContext.SaleTransactions
            .Where(s => storeIds.Contains(s.StoreId))
            .GroupBy(s => s.StoreId)
            .Select(g => new { StoreId = g.Key, LastSaleAt = g.Max(s => s.CreatedAt) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.StoreId, r => r.LastSaleAt);
    }

    public Task<int> CountAcrossPlatformInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        dbContext.SaleTransactions.CountAsync(s => s.Status == SaleStatus.Completed && s.CreatedAt >= from && s.CreatedAt < to, cancellationToken);

    public async Task<IReadOnlyList<DateTimeOffset>> GetCreatedAtAcrossPlatformInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        await dbContext.SaleTransactions
            .Where(s => s.Status == SaleStatus.Completed && s.CreatedAt >= from && s.CreatedAt < to)
            .Select(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
}

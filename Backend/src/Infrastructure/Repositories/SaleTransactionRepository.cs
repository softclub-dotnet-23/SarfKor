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
}

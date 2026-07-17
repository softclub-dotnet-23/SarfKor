using Application.Abstractions;
using Domain.Pricing;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PriceEntryRepository(AppDbContext dbContext) : IPriceEntryRepository
{
    public async Task<IReadOnlyList<PriceEntry>> GetLatestPerStoreAsync(int productId, CancellationToken cancellationToken)
    {
        var latestPerStore = dbContext.PriceEntries
            .Where(p => p.ProductId == productId)
            .GroupBy(p => p.StoreId)
            .Select(g => new { StoreId = g.Key, MaxRecordedAt = g.Max(p => p.RecordedAt) });

        var entries = dbContext.PriceEntries
            .Where(p => p.ProductId == productId)
            .Join(
                latestPerStore,
                p => new { p.StoreId, p.RecordedAt },
                l => new { l.StoreId, RecordedAt = l.MaxRecordedAt },
                (p, l) => p);

        return await entries.ToListAsync(cancellationToken);
    }

    public Task<PriceEntry?> GetLatestForStoreAsync(int productId, int storeId, CancellationToken cancellationToken) =>
        dbContext.PriceEntries
            .Where(p => p.ProductId == productId && p.StoreId == storeId)
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(PriceEntry priceEntry) => dbContext.PriceEntries.Add(priceEntry);
}

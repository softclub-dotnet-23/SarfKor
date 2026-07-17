using Application.Abstractions;
using Domain.Inventory;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CostPriceRepository(AppDbContext dbContext) : ICostPriceRepository
{
    public async Task<IReadOnlyList<CostPrice>> GetLatestForStoreAsync(int storeId, CancellationToken cancellationToken)
    {
        var latestPerProduct = dbContext.CostPrices
            .Where(c => c.StoreId == storeId)
            .GroupBy(c => c.ProductId)
            .Select(g => new { ProductId = g.Key, MaxEffectiveFrom = g.Max(c => c.EffectiveFrom) });

        var entries = dbContext.CostPrices
            .Where(c => c.StoreId == storeId)
            .Join(
                latestPerProduct,
                c => new { c.ProductId, c.EffectiveFrom },
                l => new { l.ProductId, EffectiveFrom = l.MaxEffectiveFrom },
                (c, l) => c);

        return await entries.ToListAsync(cancellationToken);
    }
}

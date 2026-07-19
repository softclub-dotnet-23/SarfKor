using Application.Abstractions;
using Domain.Analytics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ScanRepository(AppDbContext dbContext) : IScanRepository
{
    public void Add(Scan scan) => dbContext.Scans.Add(scan);

    public async Task<IReadOnlyList<ProductScanSummary>> GetMostScannedAsync(int limit, CancellationToken cancellationToken)
    {
        // Materialize first, group in memory — the same "GroupBy after a query doesn't translate
        // reliably" lesson from GetTopSellingProductsAsync (2026-07-22) applies here too.
        var productIds = await dbContext.Scans.Select(s => s.ProductId).ToListAsync(cancellationToken);

        return productIds
            .GroupBy(id => id)
            .Select(g => new ProductScanSummary(g.Key, g.Count()))
            .OrderByDescending(x => x.TotalScans)
            .Take(limit)
            .ToList();
    }
}

using Application.Abstractions;
using Domain.Analytics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ScanRepository(AppDbContext dbContext) : IScanRepository
{
    public void Add(Scan scan) => dbContext.Scans.Add(scan);

    public async Task<IReadOnlyList<ProductScanSummary>> GetMostScannedAsync(int limit, CancellationToken cancellationToken) =>
        await dbContext.Scans
            .GroupBy(s => s.ProductId)
            .Select(g => new ProductScanSummary(g.Key, g.Count()))
            .OrderByDescending(x => x.TotalScans)
            .Take(limit)
            .ToListAsync(cancellationToken);
}

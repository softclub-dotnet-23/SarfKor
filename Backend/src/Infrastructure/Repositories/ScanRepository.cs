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
        // EF Core cannot translate positional-record constructors inside a GroupBy Select.
        // Project to an anonymous type (which EF Core CAN translate to SQL), then materialize
        // into the domain record in-memory after the database round-trip.
        var rows = await dbContext.Scans
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, TotalScans = g.Count() })
            .OrderByDescending(x => x.TotalScans)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ProductScanSummary(r.ProductId, r.TotalScans)).ToList();
    }
}

using Application.Abstractions;
using Domain.Analytics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ScanRepository(AppDbContext dbContext) : IScanRepository
{
    public void Add(Scan scan) => dbContext.Scans.Add(scan);

    // Ordering by g.Count() on the grouping itself (before projecting into ProductScanSummary) is
    // what makes this translatable — Npgsql/EF Core can push GROUP BY + ORDER BY COUNT(*) DESC +
    // LIMIT down to SQL, but ordering by the *already-selected* DTO's TotalScans property (a plain
    // record member, not an aggregate EF can see through) cannot be translated and threw a 500 on
    // every call to this endpoint.
    public async Task<IReadOnlyList<ProductScanSummary>> GetMostScannedAsync(int limit, CancellationToken cancellationToken) =>
        await dbContext.Scans
            .GroupBy(s => s.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => new ProductScanSummary(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

    public Task<int> CountDistinctUsersInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        dbContext.Scans
            .Where(s => s.UserId != null && s.ScannedAt >= from && s.ScannedAt < to)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
}

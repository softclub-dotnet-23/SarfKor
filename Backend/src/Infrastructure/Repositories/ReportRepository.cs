using Application.Abstractions;
using Domain.Feedback;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ReportRepository(AppDbContext dbContext) : IReportRepository
{
    public void Add(Report report) => dbContext.Reports.Add(report);

    public Task<int> CountByUserIdSinceAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        dbContext.Reports.CountAsync(r => r.UserId == userId && r.CreatedAt >= since, cancellationToken);

    public Task<int> CountByStoreIdSinceAsync(int storeId, DateTimeOffset since, CancellationToken cancellationToken) =>
        dbContext.Reports.CountAsync(r => r.StoreId == storeId && r.CreatedAt >= since, cancellationToken);

    public async Task<IReadOnlyList<(int StoreId, int ReportCount)>> GetMostReportedStoresSinceAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Reports
            .Where(r => r.StoreId != null && r.CreatedAt >= since)
            .GroupBy(r => r.StoreId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.Select(r => (r.StoreId, r.Count)).ToList();
    }
}

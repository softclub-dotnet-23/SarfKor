using Domain.Analytics;

namespace Application.Abstractions;

public sealed record ProductScanSummary(int ProductId, int TotalScans);

public interface IScanRepository
{
    void Add(Scan scan);
    Task<IReadOnlyList<ProductScanSummary>> GetMostScannedAsync(int limit, CancellationToken cancellationToken);

    /// <summary>"Активные пользователи B2C" (ADMIN_PROMPT.md §2.5) — distinct signed-in scanners in
    /// the window; anonymous scans (UserId null) aren't counted, there's no user to be "active."</summary>
    Task<int> CountDistinctUsersInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

using Domain.Analytics;

namespace Application.Abstractions;

public sealed record ProductScanSummary(int ProductId, int TotalScans);

public interface IScanRepository
{
    void Add(Scan scan);
    Task<IReadOnlyList<ProductScanSummary>> GetMostScannedAsync(int limit, CancellationToken cancellationToken);
}

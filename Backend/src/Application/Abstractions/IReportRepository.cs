using Domain.Feedback;

namespace Application.Abstractions;

public interface IReportRepository
{
    void Add(Report report);

    /// <summary>Feeds the automatic trust-score penalty (ADMIN_PROMPT.md §1/§2.4) — accumulated
    /// reports against the same user's contributions, not a single one, is the signal.</summary>
    Task<int> CountByUserIdSinceAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Feeds the "problem stores" admin dashboard signal (ADMIN_PROMPT.md §1/§2.5).</summary>
    Task<int> CountByStoreIdSinceAsync(int storeId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Stores with the most reports since <paramref name="since"/>, most-reported first —
    /// the actual "проблемные магазины" ranking (§2.5), not just a single store's count.</summary>
    Task<IReadOnlyList<(int StoreId, int ReportCount)>> GetMostReportedStoresSinceAsync(DateTimeOffset since, int limit, CancellationToken cancellationToken);
}

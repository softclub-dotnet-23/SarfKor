using Domain.Reputation;

namespace Application.Abstractions;

public interface IContributorTrustScoreRepository
{
    Task<ContributorTrustScore?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(ContributorTrustScore trustScore);
    Task<IReadOnlyList<ContributorTrustScore>> GetAllAsync(int skip, int take, CancellationToken cancellationToken);
    Task<int> CountAllAsync(CancellationToken cancellationToken);

    /// <summary>Every row, unpaged — used only by the nightly decay job (ADMIN_PROMPT.md §2.4:
    /// "давние события весят меньше свежих"), which walks the whole table once a day.</summary>
    Task<IReadOnlyList<ContributorTrustScore>> GetAllForDecayAsync(CancellationToken cancellationToken);
}

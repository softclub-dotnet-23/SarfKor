using Domain.Reputation;

namespace Application.Abstractions;

public interface IContributorTrustScoreRepository
{
    Task<ContributorTrustScore?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(ContributorTrustScore trustScore);
}

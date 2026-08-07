using Domain.Reputation;

namespace Application.Abstractions;

public interface IContributorTrustScoreAdjustmentRepository
{
    void Add(ContributorTrustScoreAdjustment adjustment);
    Task<IReadOnlyList<ContributorTrustScoreAdjustment>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
}

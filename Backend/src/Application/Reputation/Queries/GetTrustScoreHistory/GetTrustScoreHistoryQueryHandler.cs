using Application.Abstractions;
using Application.Common;

namespace Application.Reputation.Queries.GetTrustScoreHistory;

public sealed class GetTrustScoreHistoryQueryHandler(
    IContributorTrustScoreRepository trustScoreRepository,
    IContributorTrustScoreAdjustmentRepository adjustmentRepository) : IQueryHandler<GetTrustScoreHistoryQuery, GetTrustScoreHistoryResult>
{
    public async Task<GetTrustScoreHistoryResult> Handle(GetTrustScoreHistoryQuery query, CancellationToken cancellationToken)
    {
        var score = await trustScoreRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var adjustments = await adjustmentRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var dtos = adjustments.Select(a => new TrustScoreAdjustmentDto(a.Delta, a.Reason, a.IsManual, a.PerformedByAdminUserId, a.OccurredAt)).ToList();
        return new GetTrustScoreHistoryResult(score?.Score, dtos);
    }
}

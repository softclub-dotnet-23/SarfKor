using Application.Abstractions;
using Application.Common;

namespace Application.Reputation.Queries.GetTrustScores;

public sealed class GetTrustScoresQueryHandler(
    IContributorTrustScoreRepository trustScoreRepository,
    IAuthService authService) : IQueryHandler<GetTrustScoresQuery, GetTrustScoresResult>
{
    public async Task<GetTrustScoresResult> Handle(GetTrustScoresQuery query, CancellationToken cancellationToken)
    {
        var scores = await trustScoreRepository.GetAllAsync(query.Skip, query.Take, cancellationToken);
        var totalCount = await trustScoreRepository.CountAllAsync(cancellationToken);

        var emails = await authService.GetEmailsByUserIdsAsync(scores.Select(s => s.UserId).ToList(), cancellationToken);
        var dtos = scores.Select(s => new TrustScoreListItemDto(s.UserId, emails.GetValueOrDefault(s.UserId), s.Score, s.UpdatedAt)).ToList();

        return new GetTrustScoresResult(dtos, totalCount);
    }
}

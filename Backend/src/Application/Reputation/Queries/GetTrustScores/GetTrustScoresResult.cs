namespace Application.Reputation.Queries.GetTrustScores;

public sealed record TrustScoreListItemDto(string UserId, string? Email, double Score, DateTimeOffset UpdatedAt);

public sealed record GetTrustScoresResult(IReadOnlyList<TrustScoreListItemDto> Scores, int TotalCount);

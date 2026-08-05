namespace Application.Reputation.Queries.GetTrustScoreHistory;

public sealed record TrustScoreAdjustmentDto(
    double Delta, string Reason, bool IsManual, string? PerformedByAdminUserId, DateTimeOffset OccurredAt);

public sealed record GetTrustScoreHistoryResult(double? CurrentScore, IReadOnlyList<TrustScoreAdjustmentDto> History);

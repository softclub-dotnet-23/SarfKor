namespace Application.Identity.Queries.GetUserDetail;

public enum GetUserDetailOutcome
{
    Found,
    NotFound
}

public sealed record UserStoreAttachmentDto(int StoreId, string StoreName, string Relationship);

public sealed record GetUserDetailResult(
    GetUserDetailOutcome Outcome,
    string UserId,
    string? Email,
    DateTimeOffset? CreatedAt,
    bool IsBlocked,
    string? BlockedReason,
    DateTimeOffset? BlockedAt,
    IReadOnlyList<string> Roles,
    double? TrustScore,
    int PriceSubmissionsTotal,
    int PriceSubmissionsVerified,
    int ReportsAgainstLast90Days,
    IReadOnlyList<UserStoreAttachmentDto> Stores);

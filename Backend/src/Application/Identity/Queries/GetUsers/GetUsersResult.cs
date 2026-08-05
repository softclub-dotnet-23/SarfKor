namespace Application.Identity.Queries.GetUsers;

public sealed record AdminUserListItemDto(
    string UserId,
    string? Email,
    DateTimeOffset CreatedAt,
    bool IsBlocked,
    IReadOnlyList<string> Roles,
    double? TrustScore);

public sealed record GetUsersResult(IReadOnlyList<AdminUserListItemDto> Users, int TotalCount);

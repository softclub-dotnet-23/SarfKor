using Domain.Stores;

namespace Application.Identity.Queries.GetUserInvitations;

/// <summary>StoreName/EmployeeRole are only set when InvitedRole is "StorePartner".</summary>
public sealed record UserInvitationListItem(
    int InvitationId,
    string Email,
    string InvitedRole,
    int? StoreId,
    string? StoreName,
    StoreEmployeeRole? EmployeeRole,
    StoreEmployeeInvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSentAt);

public enum GetUserInvitationsOutcome
{
    Found,
    Forbidden
}

public sealed record GetUserInvitationsResult(GetUserInvitationsOutcome Outcome, IReadOnlyList<UserInvitationListItem>? Invitations);

using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeeInvitations;

public sealed record StoreEmployeeInvitationDto(
    int InvitationId,
    string Email,
    StoreEmployeeRole Role,
    StoreEmployeeInvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSentAt);

public enum GetStoreEmployeeInvitationsOutcome
{
    Found,
    Forbidden,
    StoreNotFound
}

public sealed record GetStoreEmployeeInvitationsResult(GetStoreEmployeeInvitationsOutcome Outcome, IReadOnlyList<StoreEmployeeInvitationDto>? Invitations);

namespace Application.Identity.Commands.CreateUserInvitation;

public enum CreateUserInvitationOutcome
{
    Sent,
    Forbidden,
    StoreNotFound
}

public sealed record CreateUserInvitationResult(CreateUserInvitationOutcome Outcome, int? InvitationId, DateTimeOffset? ExpiresAt = null);

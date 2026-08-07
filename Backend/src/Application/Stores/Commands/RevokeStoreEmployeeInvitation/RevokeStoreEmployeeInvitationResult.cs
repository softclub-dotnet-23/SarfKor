namespace Application.Stores.Commands.RevokeStoreEmployeeInvitation;

public enum RevokeStoreEmployeeInvitationOutcome
{
    Revoked,
    NotFound,
    Forbidden,
    NotPending
}

public sealed record RevokeStoreEmployeeInvitationResult(RevokeStoreEmployeeInvitationOutcome Outcome);

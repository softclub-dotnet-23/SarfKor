namespace Application.Stores.Commands.ResendStoreEmployeeInvitation;

public enum ResendStoreEmployeeInvitationOutcome
{
    Resent,
    NotFound,
    Forbidden,
    NotPending
}

public sealed record ResendStoreEmployeeInvitationResult(ResendStoreEmployeeInvitationOutcome Outcome);

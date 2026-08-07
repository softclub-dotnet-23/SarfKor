using Application.Abstractions;

namespace Application.Stores.Commands.AcceptStoreEmployeeInvitation;

public enum AcceptStoreEmployeeInvitationOutcome
{
    Accepted,
    AccountAlreadyExisted,
    NotFound,
    Expired,
    AlreadyAccepted,
    Revoked,
    PasswordRequired,
    RegistrationFailed
}

/// <summary>Auth is only populated for Accepted (a brand-new account) — lets the frontend log the new
/// cashier straight in instead of sending them to /login separately.</summary>
public sealed record AcceptStoreEmployeeInvitationResult(AcceptStoreEmployeeInvitationOutcome Outcome, AuthResult? Auth);

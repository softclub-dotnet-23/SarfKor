using Application.Abstractions;

namespace Application.Identity.Commands.ConfirmAdminInvitation;

public enum ConfirmAdminInvitationOutcome
{
    Confirmed,
    InvalidOrExpiredCode,
    TooManyAttempts,
    EmailAlreadyRegistered,
    RegistrationFailed
}

public sealed record ConfirmAdminInvitationResult(ConfirmAdminInvitationOutcome Outcome, AuthResult? Auth);

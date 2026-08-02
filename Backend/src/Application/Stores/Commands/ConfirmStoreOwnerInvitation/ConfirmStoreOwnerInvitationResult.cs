using Application.Abstractions;

namespace Application.Stores.Commands.ConfirmStoreOwnerInvitation;

public enum ConfirmStoreOwnerInvitationOutcome
{
    Confirmed,
    InvalidOrExpiredCode,
    TooManyAttempts,
    EmailAlreadyRegistered,
    RegistrationFailed
}

public sealed record ConfirmStoreOwnerInvitationResult(ConfirmStoreOwnerInvitationOutcome Outcome, AuthResult? Auth, int? StoreId);

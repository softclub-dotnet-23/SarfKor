namespace Application.Stores.Commands.CreateCashierAccount;

public enum CreateCashierAccountOutcome
{
    Created,
    Forbidden,
    StoreNotFound,
    EmailAlreadyRegistered,
    RegistrationFailed,
    SubscriptionInactive,
}

/// <summary>Password is set only on Created, and only ever appears in this one response -- never
/// persisted in plaintext, never logged, never retrievable again afterward.</summary>
public sealed record CreateCashierAccountResult(CreateCashierAccountOutcome Outcome, string? Email = null, string? Password = null);

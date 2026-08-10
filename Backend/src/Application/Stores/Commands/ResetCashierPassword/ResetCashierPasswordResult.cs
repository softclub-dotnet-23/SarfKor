namespace Application.Stores.Commands.ResetCashierPassword;

public enum ResetCashierPasswordOutcome
{
    Reset,
    NotFound,
    Forbidden,
    SubscriptionInactive,
}

/// <summary>Password is set only on Reset, and only ever appears in this one response.</summary>
public sealed record ResetCashierPasswordResult(ResetCashierPasswordOutcome Outcome, string? Password = null);

namespace Application.Stores.Commands.SetStoreEmployeeActive;

public enum SetStoreEmployeeActiveOutcome
{
    Updated,
    NotFound,
    Forbidden,
    SubscriptionInactive,
    CannotDisableSelf,
}

public sealed record SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome Outcome);

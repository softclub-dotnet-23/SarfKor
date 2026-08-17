namespace Application.Stores.Commands.RemoveStoreEmployee;

public enum RemoveStoreEmployeeOutcome
{
    Removed,
    NotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome Outcome);

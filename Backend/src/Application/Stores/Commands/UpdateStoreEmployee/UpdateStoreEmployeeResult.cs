namespace Application.Stores.Commands.UpdateStoreEmployee;

public enum UpdateStoreEmployeeOutcome
{
    Updated,
    NotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record UpdateStoreEmployeeResult(UpdateStoreEmployeeOutcome Outcome);

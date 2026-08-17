namespace Application.Stores.Commands.UpdateStore;

public enum UpdateStoreOutcome
{
    Updated,
    StoreNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record UpdateStoreResult(UpdateStoreOutcome Outcome);

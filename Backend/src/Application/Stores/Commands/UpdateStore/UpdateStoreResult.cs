namespace Application.Stores.Commands.UpdateStore;

public enum UpdateStoreOutcome
{
    Updated,
    StoreNotFound,
    Forbidden
}

public sealed record UpdateStoreResult(UpdateStoreOutcome Outcome);

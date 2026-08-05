namespace Application.Stores.Commands.ChangeStoreStatus;

public enum ChangeStoreStatusOutcome
{
    Changed,
    NotFound,
    IllegalTransition
}

public sealed record ChangeStoreStatusResult(ChangeStoreStatusOutcome Outcome);

namespace Application.Stores.Commands.AddStoreEmployee;

public enum AddStoreEmployeeOutcome
{
    Added,
    StoreNotFound,
    Forbidden,
    AlreadyEmployed,
    Invited
}

public sealed record AddStoreEmployeeResult(AddStoreEmployeeOutcome Outcome, int? StoreEmployeeId);

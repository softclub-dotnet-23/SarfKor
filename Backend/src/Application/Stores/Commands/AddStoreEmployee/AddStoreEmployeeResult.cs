namespace Application.Stores.Commands.AddStoreEmployee;

public enum AddStoreEmployeeOutcome
{
    Added,
    StoreNotFound,
    Forbidden,
    AlreadyEmployed
}

public sealed record AddStoreEmployeeResult(AddStoreEmployeeOutcome Outcome, int? StoreEmployeeId);

namespace Application.Stores.Commands.RemoveStoreEmployee;

public enum RemoveStoreEmployeeOutcome
{
    Removed,
    NotFound,
    Forbidden
}

public sealed record RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome Outcome);

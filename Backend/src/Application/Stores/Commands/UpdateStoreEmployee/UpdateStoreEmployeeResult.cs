namespace Application.Stores.Commands.UpdateStoreEmployee;

public enum UpdateStoreEmployeeOutcome
{
    Updated,
    NotFound,
    Forbidden
}

public sealed record UpdateStoreEmployeeResult(UpdateStoreEmployeeOutcome Outcome);

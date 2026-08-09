namespace Application.Stores.Commands.SetStoreEmployeeActive;

public enum SetStoreEmployeeActiveOutcome
{
    Updated,
    NotFound,
    Forbidden,
}

public sealed record SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome Outcome);

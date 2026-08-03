namespace Application.Assistant.Commands.ConfirmAssistantAction;

public enum ConfirmAssistantActionOutcome
{
    Confirmed,
    /// <summary>Idempotent replay of an already-confirmed action -- same 200 result, no re-execution.</summary>
    AlreadyConfirmed,
    NotFound,
    Forbidden,
    Expired,
    FeatureDisabled,
    ExecutionFailed,
}

public sealed record ConfirmAssistantActionResult(ConfirmAssistantActionOutcome Outcome, string? Summary);

using Domain.Assistant;

namespace Application.Assistant.Abstractions;

public sealed record PendingActionExecutionResult(bool Success, string Summary);

/// <summary>
/// Executes exactly one <see cref="AssistantActionType"/> by calling the real, already-existing
/// Command handler it wraps (ConfirmAssistantActionCommandHandler dispatches to whichever executor
/// matches the pending action's type). Never called from the chat loop itself — only from a
/// separately confirmed request.
/// </summary>
public interface IPendingActionExecutor
{
    AssistantActionType ActionType { get; }
    Task<PendingActionExecutionResult> ExecuteAsync(string parametersJson, string userId, int storeId, CancellationToken cancellationToken);
}

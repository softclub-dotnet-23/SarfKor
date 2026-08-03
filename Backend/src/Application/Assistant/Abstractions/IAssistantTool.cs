namespace Application.Assistant.Abstractions;

/// <summary>
/// One capability the assistant can invoke — always a thin wrapper over an existing
/// Command/Query handler, never a new path to data. <see cref="ExecuteAsync"/> receives the
/// server-derived <see cref="AssistantCallerContext"/>, not the model's raw tool-call JSON, for
/// StoreId/UserId specifically — the input JSON only ever carries business parameters.
/// </summary>
public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    string InputSchemaJson { get; }

    /// <summary>Checked twice: once to decide whether to even advertise this tool to the model
    /// for this caller (AssistantToolRegistry), and again defensively inside ExecuteAsync before
    /// doing anything — a tool must never trust that only permitted callers can reach it.</summary>
    bool IsAvailableFor(AssistantCallerContext context);

    Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken);
}

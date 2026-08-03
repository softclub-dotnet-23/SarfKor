namespace Application.Assistant.Abstractions;

/// <summary>Structured info about a Mode C proposal, surfaced up to the API response (not just as
/// text fed back to the model) so the frontend can render a dedicated Confirm button instead of
/// having to parse the model's prose.</summary>
public sealed record ProposedActionDto(int PendingActionId, string ActionType, string Summary, DateTimeOffset ExpiresAt);

/// <summary>What a tool call actually produces: text the model sees as the tool_result, plus
/// (only for Mode C "Propose*" tools) the structured proposal it just created.</summary>
public sealed record AssistantToolExecutionResult(string TextForModel, ProposedActionDto? ProposedAction = null);

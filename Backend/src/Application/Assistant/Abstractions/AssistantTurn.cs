namespace Application.Assistant.Abstractions;

/// <summary>
/// One entry in a chat transcript, shaped so the Application layer can drive a provider-agnostic
/// tool-calling loop (AskAssistantCommandHandler) without knowing Anthropic's specific wire format —
/// Infrastructure's IAssistantChatClient implementation is the only place that translates to/from it.
/// </summary>
public abstract record AssistantTurn;

public sealed record UserTextTurn(string Text) : AssistantTurn;

public sealed record AssistantTextTurn(string Text) : AssistantTurn;

/// <summary>The model asking to invoke one tool. ToolUseId round-trips into the matching ToolResultTurn.</summary>
public sealed record AssistantToolUseTurn(string ToolUseId, string ToolName, string InputJson) : AssistantTurn;

/// <summary>The result of actually running a tool the model asked for — always sent back as data, never as an instruction.</summary>
public sealed record ToolResultTurn(string ToolUseId, string ResultText) : AssistantTurn;

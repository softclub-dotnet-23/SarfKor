namespace Application.Assistant.Abstractions;

/// <summary>
/// The one seam between Application and whichever LLM provider Infrastructure wires up (Anthropic
/// Claude today) — a use-case never talks to an HTTP client or an SDK directly, only this. Swapping
/// providers means writing a new Infrastructure implementation, not touching AskAssistantCommandHandler.
/// </summary>
public interface IAssistantChatClient
{
    /// <summary>
    /// Sends the system prompt, full turn history and available tools, and returns only the *new*
    /// turns produced by this call (one or more AssistantTextTurn/AssistantToolUseTurn — never a
    /// UserTextTurn/ToolResultTurn, those only ever come from the caller side of the loop).
    /// </summary>
    Task<IReadOnlyList<AssistantTurn>> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<AssistantTurn> conversation,
        IReadOnlyList<AssistantToolDefinition> tools,
        CancellationToken cancellationToken);
}

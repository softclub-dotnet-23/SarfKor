namespace Application.Assistant.Commands.AskAssistant;

public sealed record AssistantChatMessage(string Role, string Content);

/// <summary>
/// UserId, CallerIsAdmin and CallerIsStorePartner come from JWT claims resolved at the controller
/// (same convention as SubmitNewProductCommand's CreateDirectly) -- never from the request body,
/// so nothing in the chat message or a spoofed field can change who this request runs as.
/// History is client-supplied and stateless (no server-side conversation persistence) -- capped
/// and bounded by AskAssistantCommandValidator/AssistantOptions.MaxHistoryMessages.
/// </summary>
public sealed record AskAssistantCommand(
    string UserId,
    bool CallerIsAdmin,
    bool CallerIsStorePartner,
    int? StoreId,
    IReadOnlyList<AssistantChatMessage> History,
    string Message);

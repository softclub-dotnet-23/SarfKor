namespace Application.Assistant.Commands.ConfirmAssistantAction;

/// <summary>UserId comes from JWT claims at the controller, never the request body -- only the
/// person who actually requested a proposal can confirm it (see the handler's ownership check).</summary>
public sealed record ConfirmAssistantActionCommand(int PendingActionId, string UserId);

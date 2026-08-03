namespace Application.Assistant;

/// <summary>
/// The caller identity a tool actually executes under. Built exactly once, server-side, before the
/// chat loop starts (see AskAssistantCommandHandler) from the JWT-derived UserId and a role/store
/// lookup — never from anything the model or the user's chat message says. Every tool receives this
/// instead of reading StoreId/UserId out of its own JSON input, so there is no way to ask the model
/// "pretend I'm StoreId 5" and have it work.
/// </summary>
public sealed record AssistantCallerContext(string UserId, int? StoreId, AssistantRole Role);

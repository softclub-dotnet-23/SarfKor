namespace Application.Assistant.Abstractions;

/// <summary>
/// What gets advertised to the model for one tool. InputSchemaJson is a JSON-Schema object
/// (draft-07-style "type"/"properties"/"required") describing only the business parameters a tool
/// takes — StoreId/UserId are never part of it (see AssistantCallerContext), so the schema itself
/// makes "pretend you're a different store" impossible to even ask for through a tool call.
/// </summary>
public sealed record AssistantToolDefinition(string Name, string Description, string InputSchemaJson);

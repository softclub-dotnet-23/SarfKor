using Application.Assistant.Abstractions;

namespace Application.Assistant.Commands.AskAssistant;

public enum AskAssistantOutcome
{
    Answered,
    StoreNotFound,
    Forbidden,
}

public sealed record AskAssistantResult(AskAssistantOutcome Outcome, string? ReplyText, ProposedActionDto? ProposedAction);

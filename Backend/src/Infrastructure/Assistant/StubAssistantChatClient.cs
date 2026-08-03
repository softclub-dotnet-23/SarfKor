using Application.Assistant.Abstractions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Assistant;

/// <summary>
/// Registered instead of AnthropicAssistantChatClient whenever Anthropic:ApiKey is blank at startup
/// (see Infrastructure/DependencyInjection.cs) -- lets the whole assistant feature build, deploy and
/// have its own orchestration logic (AskAssistantCommandHandler, tool registry, role gating) exercised
/// end to end without a real key, same spirit as SmtpEmailSender's "log instead of send" fallback.
/// It deliberately does not attempt to simulate real answers -- that would be a second, fake
/// assistant to maintain. It only ever returns one honest "not configured" message.
/// </summary>
public sealed class StubAssistantChatClient(ILogger<StubAssistantChatClient> logger) : IAssistantChatClient
{
    public Task<IReadOnlyList<AssistantTurn>> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<AssistantTurn> conversation,
        IReadOnlyList<AssistantToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Anthropic:ApiKey is not configured — the assistant is running on the stub client and cannot answer real questions.");
        IReadOnlyList<AssistantTurn> reply =
        [
            new AssistantTextTurn("ИИ-ассистент временно недоступен: на сервере не настроен API-ключ. Обратитесь к администратору магазина.")
        ];
        return Task.FromResult(reply);
    }
}

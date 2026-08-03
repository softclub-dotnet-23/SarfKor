namespace Infrastructure.Assistant;

/// <summary>Bound from the "Anthropic" config section. ApiKey blank at startup means
/// StubAssistantChatClient gets registered instead of AnthropicAssistantChatClient (see
/// Infrastructure/DependencyInjection.cs) -- mirrors SmtpEmailSender's "log instead of send"
/// fallback, just decided once at startup instead of per-call, since unlike email there's no
/// silent-degrade-per-request story that makes sense for a chat endpoint.</summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-5";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public int MaxTokens { get; set; } = 1024;
}

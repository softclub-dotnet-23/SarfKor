using System.Net.Http.Json;
using System.Text.Json;
using Application.Assistant.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Assistant;

/// <summary>
/// The one place that actually talks to Anthropic's Messages API (tool use). Everything above this
/// class (AskAssistantCommandHandler, tools, the whole loop) only knows IAssistantChatClient's
/// provider-agnostic AssistantTurn shape -- this class exists purely to translate to/from Anthropic's
/// wire format, so swapping providers later means writing one new class like this, not touching
/// Application at all.
/// </summary>
public sealed class AnthropicAssistantChatClient(HttpClient httpClient, IOptions<AnthropicOptions> options) : IAssistantChatClient
{
    private const string AnthropicVersion = "2023-06-01";

    public async Task<IReadOnlyList<AssistantTurn>> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<AssistantTurn> conversation,
        IReadOnlyList<AssistantToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["model"] = options.Value.Model,
            ["max_tokens"] = options.Value.MaxTokens,
            ["system"] = systemPrompt,
            ["messages"] = BuildMessages(conversation),
        };
        if (tools.Count > 0)
            body["tools"] = BuildTools(tools);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages") { Content = JsonContent.Create(body) };
        request.Headers.Add("x-api-key", options.Value.ApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParseResponse(doc.RootElement);
    }

    // Anthropic's Messages API groups content into "user"/"assistant" messages where each message's
    // content is an array of blocks; a tool_use block belongs to an assistant message, and its
    // matching tool_result block MUST be in the very next user message (not split across several).
    // Grouping consecutive same-role turns into one message satisfies that automatically, since
    // AskAssistantCommandHandler always appends a whole batch of tool_use turns together, then a
    // whole batch of tool_result turns together, before the next model call.
    private static List<Dictionary<string, object>> BuildMessages(IReadOnlyList<AssistantTurn> conversation)
    {
        var messages = new List<Dictionary<string, object>>();
        string? currentRole = null;
        List<Dictionary<string, object>>? currentBlocks = null;

        void Flush()
        {
            if (currentRole is null || currentBlocks is null) return;
            messages.Add(new Dictionary<string, object> { ["role"] = currentRole, ["content"] = currentBlocks });
        }

        foreach (var turn in conversation)
        {
            var (role, block) = ToBlock(turn);
            if (role != currentRole)
            {
                Flush();
                currentRole = role;
                currentBlocks = [];
            }
            currentBlocks!.Add(block);
        }
        Flush();

        return messages;
    }

    private static (string Role, Dictionary<string, object> Block) ToBlock(AssistantTurn turn) => turn switch
    {
        UserTextTurn u => ("user", new Dictionary<string, object> { ["type"] = "text", ["text"] = u.Text }),
        AssistantTextTurn a => ("assistant", new Dictionary<string, object> { ["type"] = "text", ["text"] = a.Text }),
        AssistantToolUseTurn tu => ("assistant", new Dictionary<string, object>
        {
            ["type"] = "tool_use",
            ["id"] = tu.ToolUseId,
            ["name"] = tu.ToolName,
            ["input"] = JsonSerializer.Deserialize<JsonElement>(tu.InputJson),
        }),
        ToolResultTurn tr => ("user", new Dictionary<string, object>
        {
            ["type"] = "tool_result",
            ["tool_use_id"] = tr.ToolUseId,
            ["content"] = tr.ResultText,
        }),
        _ => throw new InvalidOperationException($"Unknown turn type {turn.GetType()}"),
    };

    private static List<Dictionary<string, object>> BuildTools(IReadOnlyList<AssistantToolDefinition> tools) =>
        tools.Select(t => new Dictionary<string, object>
        {
            ["name"] = t.Name,
            ["description"] = t.Description,
            ["input_schema"] = JsonSerializer.Deserialize<JsonElement>(t.InputSchemaJson),
        }).ToList();

    private static IReadOnlyList<AssistantTurn> ParseResponse(JsonElement root)
    {
        var turns = new List<AssistantTurn>();
        if (!root.TryGetProperty("content", out var contentArray))
            return turns;

        foreach (var block in contentArray.EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();
            switch (type)
            {
                case "text":
                    turns.Add(new AssistantTextTurn(block.GetProperty("text").GetString() ?? ""));
                    break;
                case "tool_use":
                    turns.Add(new AssistantToolUseTurn(
                        block.GetProperty("id").GetString()!,
                        block.GetProperty("name").GetString()!,
                        block.GetProperty("input").GetRawText()));
                    break;
            }
        }
        return turns;
    }
}

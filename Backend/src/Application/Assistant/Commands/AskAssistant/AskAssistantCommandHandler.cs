using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Assistant.Tools;
using Application.Common;
using Domain.Stores;
using Microsoft.Extensions.Options;

namespace Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreEmployeeRepository storeEmployeeRepository,
    IAssistantChatClient chatClient,
    AssistantToolRegistry toolRegistry,
    IOptions<AssistantOptions> options) : ICommandHandler<AskAssistantCommand, AskAssistantResult>
{
    private enum ContextResolution { Ok, StoreNotFound, Forbidden }

    public async Task<AskAssistantResult> Handle(AskAssistantCommand command, CancellationToken cancellationToken)
    {
        var (resolution, context) = await ResolveContextAsync(command, cancellationToken);
        if (resolution == ContextResolution.StoreNotFound)
            return new AskAssistantResult(AskAssistantOutcome.StoreNotFound, null, null);
        if (resolution == ContextResolution.Forbidden || context is null)
            return new AskAssistantResult(AskAssistantOutcome.Forbidden, null, null);

        var tools = toolRegistry.GetToolsFor(context);
        var toolDefinitions = tools.Select(t => new AssistantToolDefinition(t.Name, t.Description, t.InputSchemaJson)).ToList();
        var systemPrompt = AssistantSystemPrompt.Build(context.Role);

        var turns = new List<AssistantTurn>();
        foreach (var message in command.History.TakeLast(options.Value.MaxHistoryMessages))
            turns.Add(message.Role == "assistant" ? new AssistantTextTurn(message.Content) : new UserTextTurn(message.Content));
        turns.Add(new UserTextTurn(command.Message));

        ProposedActionDto? proposedAction = null;
        string? finalText = null;

        for (var iteration = 0; iteration < options.Value.MaxToolIterations; iteration++)
        {
            var newTurns = await chatClient.CompleteAsync(systemPrompt, turns, toolDefinitions, cancellationToken);
            turns.AddRange(newTurns);

            var toolCalls = newTurns.OfType<AssistantToolUseTurn>().ToList();
            if (toolCalls.Count == 0)
            {
                finalText = string.Join("\n", newTurns.OfType<AssistantTextTurn>().Select(t => t.Text));
                break;
            }

            foreach (var call in toolCalls)
            {
                var execResult = await ExecuteToolAsync(call, context, cancellationToken);
                if (execResult.ProposedAction is not null)
                    proposedAction = execResult.ProposedAction;
                turns.Add(new ToolResultTurn(call.ToolUseId, execResult.TextForModel));
            }
        }

        finalText ??= "Не удалось сформировать ответ за отведённое число шагов — попробуйте переформулировать вопрос проще.";
        return new AskAssistantResult(AskAssistantOutcome.Answered, finalText, proposedAction);
    }

    // Never trusts the model's tool-call arguments for anything beyond business parameters -- the
    // tool itself is looked up in the *filtered* registry (never the raw tool list), so a tool name
    // outside this caller's permitted set can't be executed even if the model asks for it anyway.
    private async Task<AssistantToolExecutionResult> ExecuteToolAsync(AssistantToolUseTurn call, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        var tool = toolRegistry.FindAvailable(call.ToolName, context);
        if (tool is null)
            return new AssistantToolExecutionResult("Этот инструмент недоступен для вашей роли.");

        try
        {
            return await tool.ExecuteAsync(call.InputJson, context, cancellationToken);
        }
        catch
        {
            // A single tool's failure (bad model-supplied JSON, a downstream error) must not blow up
            // the whole chat turn -- it becomes a normal "couldn't do that" tool_result instead.
            return new AssistantToolExecutionResult("Не удалось выполнить это действие.");
        }
    }

    private async Task<(ContextResolution Resolution, AssistantCallerContext? Context)> ResolveContextAsync(AskAssistantCommand command, CancellationToken cancellationToken)
    {
        // Admin takes priority over StorePartner, same precedence the rest of the app uses
        // (getRoleHomeRoute on the frontend, RequireStore on the backend side) -- and is never
        // store-scoped: CLAUDE.md §7 keeps Admin to platform data, not any specific store's.
        if (command.CallerIsAdmin)
            return (ContextResolution.Ok, new AssistantCallerContext(command.UserId, null, AssistantRole.Admin));

        if (!command.CallerIsStorePartner || command.StoreId is null)
            return (ContextResolution.Forbidden, null);

        if (!await storeRepository.ExistsAsync(command.StoreId.Value, cancellationToken))
            return (ContextResolution.StoreNotFound, null);

        if (await storeAccessAuthorizer.IsOwnerAsync(command.StoreId.Value, command.UserId, cancellationToken))
            return (ContextResolution.Ok, new AssistantCallerContext(command.UserId, command.StoreId, AssistantRole.StorePartner));

        // Not the owner -- only a Cashier StoreEmployee at *this specific store* gets in. A
        // StorePartner Identity role alone (e.g. a Cashier at a *different* store) is not enough,
        // matching how IStoreAccessAuthorizer already scopes everything else per-store.
        var employeeRole = await storeEmployeeRepository.GetRoleAsync(command.StoreId.Value, command.UserId, cancellationToken);
        if (employeeRole == StoreEmployeeRole.Cashier)
            return (ContextResolution.Ok, new AssistantCallerContext(command.UserId, command.StoreId, AssistantRole.Cashier));

        return (ContextResolution.Forbidden, null);
    }
}

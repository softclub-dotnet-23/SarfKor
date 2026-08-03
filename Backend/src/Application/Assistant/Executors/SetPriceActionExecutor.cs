using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Pricing.Commands.SubmitPriceUpdate;
using Domain.Assistant;

namespace Application.Assistant.Executors;

/// <summary>Only ever called from ConfirmAssistantActionCommandHandler, after the user has
/// explicitly confirmed -- never from the chat loop itself.</summary>
public sealed class SetPriceActionExecutor(
    ICommandHandler<SubmitPriceUpdateCommand, SubmitPriceUpdateResult> handler) : IPendingActionExecutor
{
    public AssistantActionType ActionType => AssistantActionType.SetPrice;

    public async Task<PendingActionExecutionResult> ExecuteAsync(string parametersJson, string userId, int storeId, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(parametersJson);
        var productId = doc.RootElement.GetProperty("productId").GetInt32();
        var price = doc.RootElement.GetProperty("price").GetDecimal();
        var currency = doc.RootElement.GetProperty("currency").GetString() ?? "TJS";

        var result = await handler.Handle(new SubmitPriceUpdateCommand(productId, storeId, userId, price, currency), cancellationToken);
        return result.Outcome switch
        {
            SubmitPriceUpdateOutcome.Submitted => new PendingActionExecutionResult(true, $"Цена обновлена (запись #{result.PriceEntryId})."),
            SubmitPriceUpdateOutcome.ProductNotFound => new PendingActionExecutionResult(false, "Товар не найден."),
            SubmitPriceUpdateOutcome.StoreNotFound => new PendingActionExecutionResult(false, "Магазин не найден."),
            SubmitPriceUpdateOutcome.Forbidden => new PendingActionExecutionResult(false, "Нет доступа к этому магазину."),
            _ => new PendingActionExecutionResult(false, "Не удалось обновить цену."),
        };
    }
}

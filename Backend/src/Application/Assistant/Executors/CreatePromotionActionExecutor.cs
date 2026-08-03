using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Offers.Commands.CreatePromotion;
using Domain.Assistant;
using Domain.Offers;

namespace Application.Assistant.Executors;

public sealed class CreatePromotionActionExecutor(
    ICommandHandler<CreatePromotionCommand, CreatePromotionResult> handler) : IPendingActionExecutor
{
    public AssistantActionType ActionType => AssistantActionType.CreatePromotion;

    public async Task<PendingActionExecutionResult> ExecuteAsync(string parametersJson, string userId, int storeId, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(parametersJson);
        var productId = doc.RootElement.GetProperty("productId").GetInt32();
        var discountType = Enum.Parse<PromotionDiscountType>(doc.RootElement.GetProperty("discountType").GetString()!);
        var discountValue = doc.RootElement.GetProperty("discountValue").GetDecimal();
        var startsAt = doc.RootElement.GetProperty("startsAt").GetDateTimeOffset();
        var endsAt = doc.RootElement.GetProperty("endsAt").GetDateTimeOffset();

        var result = await handler.Handle(
            new CreatePromotionCommand(storeId, productId, CategoryId: null, discountType, discountValue, startsAt, endsAt, userId),
            cancellationToken);
        return result.Outcome switch
        {
            CreatePromotionOutcome.Created => new PendingActionExecutionResult(true, $"Акция создана (#{result.PromotionId})."),
            CreatePromotionOutcome.ProductNotFound => new PendingActionExecutionResult(false, "Товар не найден."),
            CreatePromotionOutcome.CategoryNotFound => new PendingActionExecutionResult(false, "Категория не найдена."),
            CreatePromotionOutcome.StoreNotFound => new PendingActionExecutionResult(false, "Магазин не найден."),
            CreatePromotionOutcome.Forbidden => new PendingActionExecutionResult(false, "Нет доступа к этому магазину."),
            _ => new PendingActionExecutionResult(false, "Не удалось создать акцию."),
        };
    }
}

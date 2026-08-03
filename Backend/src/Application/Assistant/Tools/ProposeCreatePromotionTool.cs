using System.Text.Json;
using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Common;
using Domain.Assistant;
using Domain.Offers;
using Microsoft.Extensions.Options;

namespace Application.Assistant.Tools;

public sealed class ProposeCreatePromotionTool(
    IProductRepository productRepository,
    IPendingAssistantActionRepository pendingActionRepository,
    IUnitOfWork unitOfWork,
    IOptions<AssistantOptions> options) : IAssistantTool
{
    public string Name => "propose_create_promotion";
    public string Description => "Предлагает создать акцию на товар. Не создаёт акцию сразу — создаёт предложение, которое пользователь должен подтвердить в интерфейсе.";

    public string InputSchemaJson =>
        """
        {"type":"object","properties":{
          "productId":{"type":"integer","description":"Товар, на который действует акция"},
          "discountType":{"type":"string","enum":["PercentageOff","FixedAmountOff","BuyOneGetOne"]},
          "discountValue":{"type":"number","exclusiveMinimum":0},
          "startsAt":{"type":"string","description":"Начало акции, ISO 8601"},
          "endsAt":{"type":"string","description":"Конец акции, ISO 8601"}
        },"required":["productId","discountType","discountValue","startsAt","endsAt"]}
        """;

    // Owner-only: mirrors CreatePromotionCommandHandler's own IsOwnerAsync gate.
    public bool IsAvailableFor(AssistantCallerContext context) =>
        options.Value.ActionsEnabled && context.StoreId is not null && context.Role == AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Создание акций через ассистента сейчас отключено.");

        using var doc = JsonDocument.Parse(inputJson);
        if (!doc.RootElement.TryGetProperty("productId", out var productIdEl) || !productIdEl.TryGetInt32(out var productId))
            return new AssistantToolExecutionResult("Укажите товар (productId).");
        if (!doc.RootElement.TryGetProperty("discountType", out var typeEl) ||
            !Enum.TryParse<PromotionDiscountType>(typeEl.GetString(), out var discountType))
            return new AssistantToolExecutionResult("Укажите тип скидки: PercentageOff, FixedAmountOff или BuyOneGetOne.");
        if (!doc.RootElement.TryGetProperty("discountValue", out var valueEl) || valueEl.GetDecimal() <= 0)
            return new AssistantToolExecutionResult("Размер скидки должен быть больше нуля.");
        var discountValue = valueEl.GetDecimal();
        if (!doc.RootElement.TryGetProperty("startsAt", out var startEl) || !DateTimeOffset.TryParse(startEl.GetString(), out var startsAt))
            return new AssistantToolExecutionResult("Укажите дату начала акции.");
        if (!doc.RootElement.TryGetProperty("endsAt", out var endEl) || !DateTimeOffset.TryParse(endEl.GetString(), out var endsAt))
            return new AssistantToolExecutionResult("Укажите дату окончания акции.");
        if (endsAt <= startsAt)
            return new AssistantToolExecutionResult("Дата окончания должна быть позже даты начала.");

        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null) return new AssistantToolExecutionResult("Товар не найден.");

        var parametersJson = JsonSerializer.Serialize(new { productId, discountType = discountType.ToString(), discountValue, startsAt, endsAt });
        var summary = $"Создать акцию на «{product.Name}»: {discountType} {discountValue}, с {startsAt:yyyy-MM-dd} по {endsAt:yyyy-MM-dd}";
        var pendingAction = new PendingAssistantAction
        {
            RequestedByUserId = context.UserId,
            StoreId = context.StoreId!.Value,
            ActionType = AssistantActionType.CreatePromotion,
            ParametersJson = parametersJson,
            Summary = summary,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.PendingActionLifetimeMinutes),
        };
        pendingActionRepository.Add(pendingAction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var proposed = new ProposedActionDto(pendingAction.Id, nameof(AssistantActionType.CreatePromotion), summary, pendingAction.ExpiresAt);
        return new AssistantToolExecutionResult($"Предложено: {summary}. Ожидает подтверждения пользователя в интерфейсе.", proposed);
    }
}

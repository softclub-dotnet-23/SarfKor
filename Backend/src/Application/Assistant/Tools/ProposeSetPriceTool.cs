using System.Text.Json;
using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Common;
using Domain.Assistant;
using Microsoft.Extensions.Options;

namespace Application.Assistant.Tools;

/// <summary>
/// Mode C: never changes a price itself -- only drafts a PendingAssistantAction for
/// ConfirmAssistantActionCommand to execute later, and only once, against the real
/// SubmitPriceUpdateCommand handler (see SetPriceActionExecutor).
/// </summary>
public sealed class ProposeSetPriceTool(
    IProductRepository productRepository,
    IPendingAssistantActionRepository pendingActionRepository,
    IUnitOfWork unitOfWork,
    IOptions<AssistantOptions> options) : IAssistantTool
{
    public string Name => "propose_set_price";
    public string Description => "Предлагает установить цену продажи товара. Не меняет цену сразу — создаёт предложение, которое пользователь должен подтвердить в интерфейсе.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"productId":{"type":"integer"},"price":{"type":"number","exclusiveMinimum":0},"currency":{"type":"string","description":"Код валюты, например TJS"}},"required":["productId","price"]}""";

    public bool IsAvailableFor(AssistantCallerContext context) =>
        options.Value.ActionsEnabled && context.StoreId is not null && context.Role is AssistantRole.Cashier or AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Изменение цены через ассистента сейчас отключено.");

        using var doc = JsonDocument.Parse(inputJson);
        if (!doc.RootElement.TryGetProperty("productId", out var productIdEl) || !productIdEl.TryGetInt32(out var productId))
            return new AssistantToolExecutionResult("Укажите товар (productId).");
        if (!doc.RootElement.TryGetProperty("price", out var priceEl) || priceEl.GetDecimal() <= 0)
            return new AssistantToolExecutionResult("Цена должна быть больше нуля.");
        var price = priceEl.GetDecimal();
        var currency = doc.RootElement.TryGetProperty("currency", out var currEl) ? currEl.GetString() ?? "TJS" : "TJS";
        if (!SupportedCurrencies.IsSupported(currency))
            return new AssistantToolExecutionResult($"Валюта {currency} не поддерживается.");

        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null) return new AssistantToolExecutionResult("Товар не найден.");

        var parametersJson = JsonSerializer.Serialize(new { productId, price, currency });
        var summary = $"Установить цену «{product.Name}» на {price} {currency}";
        var pendingAction = new PendingAssistantAction
        {
            RequestedByUserId = context.UserId,
            StoreId = context.StoreId!.Value,
            ActionType = AssistantActionType.SetPrice,
            ParametersJson = parametersJson,
            Summary = summary,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.PendingActionLifetimeMinutes),
        };
        pendingActionRepository.Add(pendingAction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var proposed = new ProposedActionDto(pendingAction.Id, nameof(AssistantActionType.SetPrice), summary, pendingAction.ExpiresAt);
        return new AssistantToolExecutionResult($"Предложено: {summary}. Ожидает подтверждения пользователя в интерфейсе.", proposed);
    }
}

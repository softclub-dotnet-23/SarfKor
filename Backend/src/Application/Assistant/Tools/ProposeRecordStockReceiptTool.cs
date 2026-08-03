using System.Text.Json;
using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Common;
using Domain.Assistant;
using Microsoft.Extensions.Options;

namespace Application.Assistant.Tools;

public sealed class ProposeRecordStockReceiptTool(
    IProductRepository productRepository,
    IPendingAssistantActionRepository pendingActionRepository,
    IUnitOfWork unitOfWork,
    IOptions<AssistantOptions> options) : IAssistantTool
{
    public string Name => "propose_record_stock_receipt";
    public string Description => "Предлагает оприходовать поставку товара. Не меняет остаток сразу — создаёт предложение, которое пользователь должен подтвердить в интерфейсе.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"productId":{"type":"integer"},"quantity":{"type":"integer","exclusiveMinimum":0}},"required":["productId","quantity"]}""";

    public bool IsAvailableFor(AssistantCallerContext context) =>
        options.Value.ActionsEnabled && context.StoreId is not null && context.Role is AssistantRole.Cashier or AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Оприходование через ассистента сейчас отключено.");

        using var doc = JsonDocument.Parse(inputJson);
        if (!doc.RootElement.TryGetProperty("productId", out var productIdEl) || !productIdEl.TryGetInt32(out var productId))
            return new AssistantToolExecutionResult("Укажите товар (productId).");
        if (!doc.RootElement.TryGetProperty("quantity", out var qtyEl) || !qtyEl.TryGetInt32(out var quantity) || quantity <= 0)
            return new AssistantToolExecutionResult("Количество должно быть положительным целым числом.");

        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null) return new AssistantToolExecutionResult("Товар не найден.");

        var parametersJson = JsonSerializer.Serialize(new { productId, quantity });
        var summary = $"Оприходовать поставку «{product.Name}»: {quantity} шт.";
        var pendingAction = new PendingAssistantAction
        {
            RequestedByUserId = context.UserId,
            StoreId = context.StoreId!.Value,
            ActionType = AssistantActionType.RecordStockReceipt,
            ParametersJson = parametersJson,
            Summary = summary,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.PendingActionLifetimeMinutes),
        };
        pendingActionRepository.Add(pendingAction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var proposed = new ProposedActionDto(pendingAction.Id, nameof(AssistantActionType.RecordStockReceipt), summary, pendingAction.ExpiresAt);
        return new AssistantToolExecutionResult($"Предложено: {summary}. Ожидает подтверждения пользователя в интерфейсе.", proposed);
    }
}

using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Inventory.Commands.RecordStockReceipt;
using Domain.Assistant;

namespace Application.Assistant.Executors;

public sealed class RecordStockReceiptActionExecutor(
    ICommandHandler<RecordStockReceiptCommand, RecordStockReceiptResult> handler) : IPendingActionExecutor
{
    public AssistantActionType ActionType => AssistantActionType.RecordStockReceipt;

    public async Task<PendingActionExecutionResult> ExecuteAsync(string parametersJson, string userId, int storeId, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(parametersJson);
        var productId = doc.RootElement.GetProperty("productId").GetInt32();
        var quantity = doc.RootElement.GetProperty("quantity").GetInt32();

        var result = await handler.Handle(new RecordStockReceiptCommand(storeId, productId, quantity, userId, SupplierId: null), cancellationToken);
        return result.Outcome switch
        {
            RecordStockReceiptOutcome.Received => new PendingActionExecutionResult(true, $"Поставка оприходована (движение #{result.StockMovementId})."),
            RecordStockReceiptOutcome.ProductNotFound => new PendingActionExecutionResult(false, "Товар не найден."),
            RecordStockReceiptOutcome.StoreNotFound => new PendingActionExecutionResult(false, "Магазин не найден."),
            RecordStockReceiptOutcome.Forbidden => new PendingActionExecutionResult(false, "Нет доступа к этому магазину."),
            _ => new PendingActionExecutionResult(false, "Не удалось оприходовать поставку."),
        };
    }
}

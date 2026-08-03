using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Inventory.Queries.GetReorderAlerts;

namespace Application.Assistant.Tools;

public sealed class GetReorderAlertsTool(
    IQueryHandler<GetReorderAlertsQuery, GetReorderAlertsResult> handler,
    IProductRepository productRepository) : IAssistantTool
{
    public string Name => "get_reorder_alerts";
    public string Description => "Возвращает товары, остаток которых опустился ниже порога пополнения — что скоро кончится.";
    public string InputSchemaJson => """{"type":"object","properties":{},"required":[]}""";

    // Owner-only: mirrors GetReorderAlertsQueryHandler's own IsOwnerAsync gate, so a Cashier never
    // even sees this tool offered rather than relying only on the handler to say Forbidden.
    public bool IsAvailableFor(AssistantCallerContext context) =>
        context.StoreId is not null && context.Role == AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        var result = await handler.Handle(new GetReorderAlertsQuery(context.StoreId!.Value, context.UserId), cancellationToken);
        if (result.Outcome != GetReorderAlertsOutcome.Found || result.Alerts is null)
            return new AssistantToolExecutionResult("Не удалось получить список товаров для пополнения.");
        if (result.Alerts.Count == 0)
            return new AssistantToolExecutionResult("Нет товаров, требующих пополнения — все остатки выше порога.");

        var products = await productRepository.GetByIdsAsync(result.Alerts.Select(a => a.ProductId).ToList(), cancellationToken);
        var namesById = products.ToDictionary(p => p.Id, p => p.Name);
        var lines = result.Alerts.Select(a =>
            $"- {namesById.GetValueOrDefault(a.ProductId, $"Товар #{a.ProductId}")}: остаток {a.CurrentQuantity}, порог {a.ThresholdQuantity}, рекомендуемый заказ {a.ReorderQuantity} шт.");
        return new AssistantToolExecutionResult(string.Join("\n", lines));
    }
}

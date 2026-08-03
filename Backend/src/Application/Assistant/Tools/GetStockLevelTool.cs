using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Inventory.Queries.GetStockLevel;

namespace Application.Assistant.Tools;

public sealed class GetStockLevelTool(
    IQueryHandler<GetStockLevelQuery, GetStockLevelResult> handler,
    IProductRepository productRepository) : IAssistantTool
{
    public string Name => "get_stock_levels";
    public string Description => "Возвращает текущие остатки всех товаров на складе магазина: название и количество.";
    public string InputSchemaJson => """{"type":"object","properties":{},"required":[]}""";

    public bool IsAvailableFor(AssistantCallerContext context) =>
        context.StoreId is not null && context.Role is AssistantRole.Cashier or AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        var result = await handler.Handle(new GetStockLevelQuery(context.StoreId!.Value, context.UserId), cancellationToken);
        if (result.Outcome != GetStockLevelOutcome.Found || result.Levels is null)
            return new AssistantToolExecutionResult("Не удалось получить остатки склада.");
        if (result.Levels.Count == 0)
            return new AssistantToolExecutionResult("На складе пока нет ни одной позиции.");

        var products = await productRepository.GetByIdsAsync(result.Levels.Select(l => l.ProductId).ToList(), cancellationToken);
        var namesById = products.ToDictionary(p => p.Id, p => p.Name);
        var lines = result.Levels.Select(l => $"- {namesById.GetValueOrDefault(l.ProductId, $"Товар #{l.ProductId}")}: {l.Quantity} шт.");
        return new AssistantToolExecutionResult(string.Join("\n", lines));
    }
}

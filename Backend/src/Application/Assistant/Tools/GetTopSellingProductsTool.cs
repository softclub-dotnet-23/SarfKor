using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Products.Queries.GetTopSellingProducts;

namespace Application.Assistant.Tools;

public sealed class GetTopSellingProductsTool(
    IQueryHandler<GetTopSellingProductsQuery, GetTopSellingProductsResult> handler) : IAssistantTool
{
    public string Name => "get_top_selling_products";
    public string Description => "Возвращает самые продаваемые товары магазина по количеству проданных штук.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"limit":{"type":"integer","description":"Сколько товаров вернуть (по умолчанию 10)","minimum":1,"maximum":50}},"required":[]}""";

    public bool IsAvailableFor(AssistantCallerContext context) =>
        context.StoreId is not null && context.Role is AssistantRole.Cashier or AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        var limit = 10;
        if (!string.IsNullOrWhiteSpace(inputJson))
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (doc.RootElement.TryGetProperty("limit", out var limitEl) && limitEl.TryGetInt32(out var parsed))
                limit = Math.Clamp(parsed, 1, 50);
        }

        var result = await handler.Handle(new GetTopSellingProductsQuery(context.StoreId, limit), cancellationToken);
        if (result.Products.Count == 0)
            return new AssistantToolExecutionResult("Продаж пока нет.");

        var lines = result.Products.Select((p, i) => $"{i + 1}. {p.ProductName} — продано {p.TotalQuantity} шт.");
        return new AssistantToolExecutionResult(string.Join("\n", lines));
    }
}

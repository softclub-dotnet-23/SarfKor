using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Sales.Queries.GetDailySalesReport;

namespace Application.Assistant.Tools;

public sealed class GetDailySalesReportTool(
    IQueryHandler<GetDailySalesReportQuery, GetDailySalesReportResult> handler) : IAssistantTool
{
    public string Name => "get_daily_sales_report";
    public string Description => "Возвращает количество продаж и выручку магазина за один конкретный день.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"date":{"type":"string","description":"Дата в формате YYYY-MM-DD, по умолчанию сегодня"}},"required":[]}""";

    // Owner-only: mirrors GetDailySalesReportQueryHandler's own IsOwnerAsync -- revenue is not
    // something a Cashier should ever see through the assistant either.
    public bool IsAvailableFor(AssistantCallerContext context) =>
        context.StoreId is not null && context.Role == AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(inputJson))
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (doc.RootElement.TryGetProperty("date", out var dateEl) &&
                DateOnly.TryParse(dateEl.GetString(), out var parsed))
                date = parsed;
        }

        var result = await handler.Handle(new GetDailySalesReportQuery(context.StoreId!.Value, date, context.UserId), cancellationToken);
        var text = result.Outcome switch
        {
            GetDailySalesReportOutcome.Found =>
                $"За {result.Date:yyyy-MM-dd}: {result.SalesCount} продаж, выручка {result.Revenue} {result.Currency}.",
            GetDailySalesReportOutcome.StoreNotFound => "Магазин не найден.",
            _ => "Не удалось получить отчёт за день.",
        };
        return new AssistantToolExecutionResult(text);
    }
}

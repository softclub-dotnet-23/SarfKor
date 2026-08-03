using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Sales.Queries.GetProfitReport;

namespace Application.Assistant.Tools;

public sealed class GetProfitReportTool(
    IQueryHandler<GetProfitReportQuery, GetProfitReportResult> handler) : IAssistantTool
{
    public string Name => "get_profit_report";
    public string Description => "Возвращает выручку, себестоимость и прибыль магазина за период. Содержит коммерческую тайну (себестоимость) — доступно только владельцу.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"fromDate":{"type":"string","description":"Начало периода, YYYY-MM-DD"},"toDate":{"type":"string","description":"Конец периода, YYYY-MM-DD"}},"required":["fromDate","toDate"]}""";

    // Deliberately StorePartner-only -- this is exactly the CostPrice/profit data CLAUDE.md §4 says a
    // Cashier must never see, directly or indirectly ("во сколько мне обошёлся товар", "наценка").
    public bool IsAvailableFor(AssistantCallerContext context) =>
        context.StoreId is not null && context.Role == AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(inputJson) ? "{}" : inputJson);
        if (!doc.RootElement.TryGetProperty("fromDate", out var fromEl) || !DateOnly.TryParse(fromEl.GetString(), out var fromDate) ||
            !doc.RootElement.TryGetProperty("toDate", out var toEl) || !DateOnly.TryParse(toEl.GetString(), out var toDate))
            return new AssistantToolExecutionResult("Укажите обе даты периода в формате YYYY-MM-DD.");

        var result = await handler.Handle(new GetProfitReportQuery(context.StoreId!.Value, fromDate, toDate, context.UserId), cancellationToken);
        var text = result.Outcome switch
        {
            GetProfitReportOutcome.Found =>
                $"С {result.FromDate:yyyy-MM-dd} по {result.ToDate:yyyy-MM-dd}: выручка {result.Revenue} {result.Currency}, себестоимость {result.TotalCost} {result.Currency}, прибыль {result.Profit} {result.Currency}.",
            GetProfitReportOutcome.StoreNotFound => "Магазин не найден.",
            _ => "Не удалось получить отчёт по прибыли.",
        };
        return new AssistantToolExecutionResult(text);
    }
}

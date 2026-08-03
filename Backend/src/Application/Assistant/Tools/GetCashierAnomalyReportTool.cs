using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Sales.Queries.GetCashierAnomalyReport;

namespace Application.Assistant.Tools;

public sealed class GetCashierAnomalyReportTool(
    IQueryHandler<GetCashierAnomalyReportQuery, GetCashierAnomalyReportResult> handler) : IAssistantTool
{
    public string Name => "get_cashier_anomaly_report";
    public string Description => "Возвращает по каждому кассиру число продаж и долю отмен за период — помогает заметить подозрительные паттерны.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"fromDate":{"type":"string","description":"Начало периода, YYYY-MM-DD"},"toDate":{"type":"string","description":"Конец периода, YYYY-MM-DD"}},"required":["fromDate","toDate"]}""";

    public bool IsAvailableFor(AssistantCallerContext context) =>
        context.StoreId is not null && context.Role == AssistantRole.StorePartner;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(inputJson) ? "{}" : inputJson);
        if (!doc.RootElement.TryGetProperty("fromDate", out var fromEl) || !DateOnly.TryParse(fromEl.GetString(), out var fromDate) ||
            !doc.RootElement.TryGetProperty("toDate", out var toEl) || !DateOnly.TryParse(toEl.GetString(), out var toDate))
            return new AssistantToolExecutionResult("Укажите обе даты периода в формате YYYY-MM-DD.");

        var result = await handler.Handle(new GetCashierAnomalyReportQuery(context.StoreId!.Value, fromDate, toDate, context.UserId), cancellationToken);
        if (result.Outcome != GetCashierAnomalyReportOutcome.Found || result.Cashiers is null)
            return new AssistantToolExecutionResult("Не удалось получить отчёт по кассирам.");
        if (result.Cashiers.Count == 0)
            return new AssistantToolExecutionResult("За этот период продаж не было.");

        var lines = result.Cashiers.Select(c =>
            $"- Кассир {c.CashierUserId}: {c.TotalSales} продаж, {c.VoidedSales} отменено ({c.VoidRate:P0}){(c.IsAnomalous ? " — аномально высокая доля отмен" : "")}.");
        return new AssistantToolExecutionResult(string.Join("\n", lines));
    }
}

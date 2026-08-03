using Application.Assistant.Abstractions;
using Application.Common;
using Application.Feedback.Queries.GetPendingReports;

namespace Application.Assistant.Tools;

public sealed class GetPendingReportsTool(
    IQueryHandler<GetPendingReportsQuery, GetPendingReportsResult> handler) : IAssistantTool
{
    public string Name => "get_pending_reports";
    public string Description => "Возвращает очередь жалоб/репортов от пользователей, ожидающих модерации (неверная цена, нет в наличии и т.п.).";
    public string InputSchemaJson => """{"type":"object","properties":{},"required":[]}""";

    public bool IsAvailableFor(AssistantCallerContext context) => context.Role == AssistantRole.Admin;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        var result = await handler.Handle(new GetPendingReportsQuery(), cancellationToken);
        if (result.Reports.Count == 0) return new AssistantToolExecutionResult("Очередь жалоб пуста.");

        var lines = result.Reports.Select(r => $"- #{r.ReportId} тип {r.Type}, товар #{r.ProductId}: {r.Description}");
        return new AssistantToolExecutionResult($"В очереди {result.Reports.Count} жалоб(а):\n" + string.Join("\n", lines));
    }
}

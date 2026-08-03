using Application.Assistant.Abstractions;
using Application.Common;
using Application.Products.Queries.GetPendingProductSubmissions;

namespace Application.Assistant.Tools;

public sealed class GetPendingProductSubmissionsTool(
    IQueryHandler<GetPendingProductSubmissionsQuery, GetPendingProductSubmissionsResult> handler) : IAssistantTool
{
    public string Name => "get_pending_product_submissions";
    public string Description => "Возвращает очередь новых товаров, ожидающих модерации.";
    public string InputSchemaJson => """{"type":"object","properties":{},"required":[]}""";

    public bool IsAvailableFor(AssistantCallerContext context) => context.Role == AssistantRole.Admin;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        var result = await handler.Handle(new GetPendingProductSubmissionsQuery(), cancellationToken);
        if (result.Submissions.Count == 0) return new AssistantToolExecutionResult("Очередь модерации товаров пуста.");

        var lines = result.Submissions.Select(s => $"- #{s.SubmissionId} «{s.Name}» (штрихкод {s.Barcode}), подано {s.CreatedAt:yyyy-MM-dd}");
        return new AssistantToolExecutionResult($"В очереди {result.Submissions.Count} товар(ов):\n" + string.Join("\n", lines));
    }
}

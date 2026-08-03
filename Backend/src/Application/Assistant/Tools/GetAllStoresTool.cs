using System.Text.Json;
using Application.Assistant.Abstractions;
using Application.Common;
using Application.Stores.Queries.GetAllStores;

namespace Application.Assistant.Tools;

public sealed class GetAllStoresTool(
    IQueryHandler<GetAllStoresQuery, GetAllStoresResult> handler) : IAssistantTool
{
    public string Name => "get_all_stores";
    public string Description => "Возвращает список магазинов на платформе с владельцами и статусом одобрения. Только платформенные данные, без выручки/остатков конкретных магазинов.";

    public string InputSchemaJson =>
        """{"type":"object","properties":{"skip":{"type":"integer","minimum":0},"take":{"type":"integer","minimum":1,"maximum":50}},"required":[]}""";

    public bool IsAvailableFor(AssistantCallerContext context) => context.Role == AssistantRole.Admin;

    public async Task<AssistantToolExecutionResult> ExecuteAsync(string inputJson, AssistantCallerContext context, CancellationToken cancellationToken)
    {
        if (!IsAvailableFor(context)) return new AssistantToolExecutionResult("Инструмент недоступен для этой роли.");

        int skip = 0, take = 20;
        if (!string.IsNullOrWhiteSpace(inputJson))
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (doc.RootElement.TryGetProperty("skip", out var skipEl) && skipEl.TryGetInt32(out var s)) skip = Math.Max(0, s);
            if (doc.RootElement.TryGetProperty("take", out var takeEl) && takeEl.TryGetInt32(out var t)) take = Math.Clamp(t, 1, 50);
        }

        var result = await handler.Handle(new GetAllStoresQuery(skip, take), cancellationToken);
        if (result.Stores.Count == 0) return new AssistantToolExecutionResult("Магазинов не найдено.");

        var lines = result.Stores.Select(s => $"- #{s.StoreId} «{s.Name}», {s.Address}, статус {s.Status}, владелец {s.OwnerEmail ?? s.OwnerUserId}");
        return new AssistantToolExecutionResult($"Всего магазинов: {result.TotalCount}.\n" + string.Join("\n", lines));
    }
}

namespace Application.Auditing.Queries.GetRecentAuditLogs;

public sealed record AuditLogDto(
    int AuditLogId,
    string PerformedByUserId,
    string Action,
    string EntityType,
    int EntityId,
    string? Details,
    DateTimeOffset OccurredAt);

public sealed record GetRecentAuditLogsResult(IReadOnlyList<AuditLogDto> Logs);

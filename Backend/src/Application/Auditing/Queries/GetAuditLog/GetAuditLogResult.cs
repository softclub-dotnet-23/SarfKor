namespace Application.Auditing.Queries.GetAuditLog;

public sealed record AuditLogEntryDto(
    int AuditLogId,
    string PerformedByUserId,
    string? PerformedByEmail,
    string Action,
    string EntityType,
    int EntityId,
    string? Details,
    string? Reason,
    string? IpAddress,
    string? BeforeStateJson,
    string? AfterStateJson,
    DateTimeOffset OccurredAt);

public sealed record GetAuditLogResult(IReadOnlyList<AuditLogEntryDto> Entries, int TotalCount);

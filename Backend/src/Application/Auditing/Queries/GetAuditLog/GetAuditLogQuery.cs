namespace Application.Auditing.Queries.GetAuditLog;

public sealed record GetAuditLogQuery(
    int Skip,
    int Take,
    string? PerformedByUserId,
    string? Action,
    string? EntityType,
    int? EntityId,
    DateTimeOffset? From,
    DateTimeOffset? To);

using Application.Abstractions;
using Application.Common;

namespace Application.Auditing.Queries.GetRecentAuditLogs;

public sealed class GetRecentAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IQueryHandler<GetRecentAuditLogsQuery, GetRecentAuditLogsResult>
{
    public async Task<GetRecentAuditLogsResult> Handle(GetRecentAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var logs = await auditLogRepository.GetRecentAsync(query.Count, cancellationToken);
        var dtos = logs
            .Select(l => new AuditLogDto(l.Id, l.PerformedByUserId, l.Action, l.EntityType, l.EntityId, l.Details, l.OccurredAt))
            .ToList();
        return new GetRecentAuditLogsResult(dtos);
    }
}

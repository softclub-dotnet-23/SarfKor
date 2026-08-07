using Application.Abstractions;
using Application.Common;

namespace Application.Auditing.Queries.GetAuditLog;

public sealed class GetAuditLogQueryHandler(
    IAuditLogRepository auditLogRepository,
    IAuthService authService) : IQueryHandler<GetAuditLogQuery, GetAuditLogResult>
{
    public async Task<GetAuditLogResult> Handle(GetAuditLogQuery query, CancellationToken cancellationToken)
    {
        var entries = await auditLogRepository.GetFilteredAsync(
            query.Skip, query.Take, query.PerformedByUserId, query.Action, query.EntityType, query.EntityId,
            query.From, query.To, cancellationToken);
        var totalCount = await auditLogRepository.CountFilteredAsync(
            query.PerformedByUserId, query.Action, query.EntityType, query.EntityId, query.From, query.To, cancellationToken);

        var emails = await authService.GetEmailsByUserIdsAsync(entries.Select(e => e.PerformedByUserId).Distinct().ToList(), cancellationToken);

        var dtos = entries.Select(e => new AuditLogEntryDto(
            e.Id, e.PerformedByUserId, emails.GetValueOrDefault(e.PerformedByUserId), e.Action, e.EntityType, e.EntityId,
            e.Details, e.Reason, e.IpAddress, e.BeforeStateJson, e.AfterStateJson, e.OccurredAt)).ToList();

        return new GetAuditLogResult(dtos, totalCount);
    }
}

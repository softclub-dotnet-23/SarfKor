using Domain.Auditing;

namespace Application.Abstractions;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken);
}

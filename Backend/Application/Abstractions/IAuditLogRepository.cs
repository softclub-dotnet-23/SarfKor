using Domain.Auditing;

namespace Application.Abstractions;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
}

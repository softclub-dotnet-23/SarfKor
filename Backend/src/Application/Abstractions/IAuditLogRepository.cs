using Domain.Auditing;

namespace Application.Abstractions;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLog>> GetFilteredAsync(
        int skip, int take, string? performedByUserId, string? action, string? entityType, int? entityId,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);

    Task<int> CountFilteredAsync(
        string? performedByUserId, string? action, string? entityType, int? entityId,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
}

using Application.Abstractions;
using Domain.Auditing;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AuditLogRepository(AppDbContext dbContext) : IAuditLogRepository
{
    public void Add(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken) =>
        await dbContext.AuditLogs.OrderByDescending(a => a.OccurredAt).Take(count).ToListAsync(cancellationToken);

    private IQueryable<AuditLog> ApplyFilter(
        string? performedByUserId, string? action, string? entityType, int? entityId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var query = dbContext.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(performedByUserId)) query = query.Where(a => a.PerformedByUserId == performedByUserId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (entityId is not null) query = query.Where(a => a.EntityId == entityId);
        if (from is not null) query = query.Where(a => a.OccurredAt >= from);
        if (to is not null) query = query.Where(a => a.OccurredAt <= to);
        return query;
    }

    public async Task<IReadOnlyList<AuditLog>> GetFilteredAsync(
        int skip, int take, string? performedByUserId, string? action, string? entityType, int? entityId,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken) =>
        await ApplyFilter(performedByUserId, action, entityType, entityId, from, to)
            .OrderByDescending(a => a.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountFilteredAsync(
        string? performedByUserId, string? action, string? entityType, int? entityId,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken) =>
        ApplyFilter(performedByUserId, action, entityType, entityId, from, to).CountAsync(cancellationToken);
}

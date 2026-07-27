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
}

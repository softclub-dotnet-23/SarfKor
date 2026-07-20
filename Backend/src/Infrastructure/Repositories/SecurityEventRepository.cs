using Application.Abstractions;
using Domain.Security;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class SecurityEventRepository(AppDbContext dbContext) : ISecurityEventRepository
{
    public async Task<IReadOnlyList<SecurityEvent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.SecurityEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    public void Add(SecurityEvent securityEvent) => dbContext.SecurityEvents.Add(securityEvent);
}

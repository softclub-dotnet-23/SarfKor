using Application.Abstractions;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AdminInvitationRepository(AppDbContext dbContext) : IAdminInvitationRepository
{
    public Task<AdminInvitation?> GetPendingByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.AdminInvitations
            .Where(i => i.Email == email && i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(AdminInvitation invitation) => dbContext.AdminInvitations.Add(invitation);
}

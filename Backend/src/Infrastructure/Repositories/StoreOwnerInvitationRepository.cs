using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreOwnerInvitationRepository(AppDbContext dbContext) : IStoreOwnerInvitationRepository
{
    public Task<StoreOwnerInvitation?> GetPendingByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.StoreOwnerInvitations
            .Where(i => i.Email == email && i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(StoreOwnerInvitation invitation) => dbContext.StoreOwnerInvitations.Add(invitation);
}

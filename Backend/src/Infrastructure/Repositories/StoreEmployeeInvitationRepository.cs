using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreEmployeeInvitationRepository(AppDbContext dbContext) : IStoreEmployeeInvitationRepository
{
    public Task<StoreEmployeeInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        dbContext.StoreEmployeeInvitations.FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public Task<StoreEmployeeInvitation?> GetPendingByStoreAndEmailAsync(int storeId, string email, CancellationToken cancellationToken) =>
        dbContext.StoreEmployeeInvitations.FirstOrDefaultAsync(
            i => i.StoreId == storeId && i.Email == email && i.AcceptedAt == null && i.ExpiresAt > DateTimeOffset.UtcNow,
            cancellationToken);

    public void Add(StoreEmployeeInvitation invitation) => dbContext.StoreEmployeeInvitations.Add(invitation);
}

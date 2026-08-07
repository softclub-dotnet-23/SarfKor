using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreEmployeeInvitationRepository(AppDbContext dbContext) : IStoreEmployeeInvitationRepository
{
    public Task<StoreEmployeeInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.StoreEmployeeInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public Task<StoreEmployeeInvitation?> GetByIdAsync(int invitationId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployeeInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

    public Task<StoreEmployeeInvitation?> GetPendingByStoreAndEmailAsync(int storeId, string email, CancellationToken cancellationToken) =>
        dbContext.StoreEmployeeInvitations.FirstOrDefaultAsync(
            i => i.StoreId == storeId && i.Email == email && i.Status == StoreEmployeeInvitationStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<StoreEmployeeInvitation>> GetByStoreIdAsync(int storeId, StoreEmployeeInvitationStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.StoreEmployeeInvitations.Where(i => i.StoreId == storeId);
        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);
        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoreEmployeeInvitation>> GetPendingExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        await dbContext.StoreEmployeeInvitations
            .Where(i => i.Status == StoreEmployeeInvitationStatus.Pending && i.ExpiresAt < now)
            .ToListAsync(cancellationToken);

    public void Add(StoreEmployeeInvitation invitation) => dbContext.StoreEmployeeInvitations.Add(invitation);
}

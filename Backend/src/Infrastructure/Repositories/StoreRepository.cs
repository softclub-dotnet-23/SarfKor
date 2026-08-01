using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreRepository(AppDbContext dbContext) : IStoreRepository
{
    public async Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => storeIds.Contains(s.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Store>> GetApprovedByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => storeIds.Contains(s.Id) && s.Status == StoreStatus.Approved).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.AnyAsync(s => s.Id == storeId, cancellationToken);

    public Task<Store?> GetByIdAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);

    public Task<bool> OwnsAnyStoreAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.Stores.AnyAsync(s => s.OwnerUserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Store>> GetOwnedByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => s.OwnerUserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Store>> GetAllAsync(int skip, int take, CancellationToken cancellationToken) =>
        await dbContext.Stores.OrderBy(s => s.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAllAsync(CancellationToken cancellationToken) =>
        dbContext.Stores.CountAsync(cancellationToken);

    public void Add(Store store) => dbContext.Stores.Add(store);
}

using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreRepository(AppDbContext dbContext) : IStoreRepository
{
    public async Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => storeIds.Contains(s.Id)).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.AnyAsync(s => s.Id == storeId, cancellationToken);

    public Task<Store?> GetByIdAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);

    public void Add(Store store) => dbContext.Stores.Add(store);
}

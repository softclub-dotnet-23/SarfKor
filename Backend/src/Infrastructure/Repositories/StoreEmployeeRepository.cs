using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreEmployeeRepository(AppDbContext dbContext) : IStoreEmployeeRepository
{
    public Task<StoreEmployee?> GetByIdAsync(int storeEmployeeId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees.FirstOrDefaultAsync(e => e.Id == storeEmployeeId, cancellationToken);

    public async Task<IReadOnlyList<StoreEmployee>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.StoreEmployees.Where(e => e.StoreId == storeId).ToListAsync(cancellationToken);

    public Task<bool> IsEmployeeAsync(int storeId, string userId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees.AnyAsync(e => e.StoreId == storeId && e.UserId == userId, cancellationToken);

    public Task<bool> IsEmployedAnywhereAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees.AnyAsync(e => e.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<StoreEmployee>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.StoreEmployees.Where(e => e.UserId == userId).ToListAsync(cancellationToken);

    public void Add(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Add(storeEmployee);

    public void Remove(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Remove(storeEmployee);
}

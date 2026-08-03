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

    // Projects only Role instead of materializing the full entity -- StoreEmployee.MonthlySalary is
    // a nullable Money complex property, and EF's complex-type materialization throws
    // (ArgumentException: "Currency must be a 3-letter code") when reconstructing Money from a row
    // where MonthlySalary_Amount/_Currency are both genuinely NULL (a Cashier added without a salary
    // set, the common case) instead of treating the whole complex property as null. Pre-existing gap
    // in StoreEmployeeConfiguration, not something this method can fix on its own -- see WORKLOG.
    // GetByIdAsync/GetByStoreIdAsync/GetByUserIdAsync below still materialize the full entity and
    // remain exposed to it.
    public Task<StoreEmployeeRole?> GetRoleAsync(int storeId, string userId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees
            .Where(e => e.StoreId == storeId && e.UserId == userId)
            .Select(e => (StoreEmployeeRole?)e.Role)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreEmployee>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.StoreEmployees.Where(e => e.UserId == userId).ToListAsync(cancellationToken);

    public void Add(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Add(storeEmployee);

    public void Remove(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Remove(storeEmployee);
}

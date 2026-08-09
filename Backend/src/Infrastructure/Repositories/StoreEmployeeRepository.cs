using Application.Abstractions;
using Domain.Stores;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreEmployeeRepository(AppDbContext dbContext) : IStoreEmployeeRepository
{
    public Task<StoreEmployee?> GetByIdAsync(int storeEmployeeId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees.FirstOrDefaultAsync(e => e.Id == storeEmployeeId, cancellationToken);

    // Same MonthlySalary trap as GetRoleAsync/GetByUserIdAsync below -- but unlike GetByUserIdAsync,
    // every caller here (GetStoreEmployeesQueryHandler in particular) genuinely needs the salary
    // when one is set, so this can't just drop the column. Instead it projects the two raw
    // MonthlySalary_Amount/_Currency columns as plain scalars -- a leaf-scalar Select translates to a
    // direct column read, never invoking Money's constructor mid-query the way materializing the
    // full complex property does -- then reconstructs Money afterward in C#, in memory, where a
    // NULL/invalid Currency is deliberately treated as "no salary set" instead of throwing. Confirmed
    // live: IsRequired(false) on StoreEmployeeConfiguration did not stop fresh inserts (e.g. a
    // brand-new cashier via CreateCashierAccountCommandHandler) from persisting Amount=0/Currency=NULL
    // instead of a clean all-NULL row, so this has to tolerate that shape on read, not just on the
    // pre-existing rows this session's data-fix already touched.
    public async Task<IReadOnlyList<StoreEmployee>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.StoreEmployees
            .Where(e => e.StoreId == storeId)
            .Select(e => new
            {
                e.Id,
                e.StoreId,
                e.UserId,
                e.Role,
                e.AddedAt,
                e.ScheduleStart,
                e.ScheduleEnd,
                // Amount must be projected as decimal?, not decimal -- Money.Amount is a non-nullable
                // decimal, but the underlying MonthlySalary_Amount column genuinely is NULL for a
                // salary-less row, and materializing a NULL column into a non-nullable decimal throws
                // "Nullable object must have a value" (confirmed live) instead of translating cleanly.
                SalaryAmount = (decimal?)e.MonthlySalary!.Amount,
                SalaryCurrency = e.MonthlySalary!.Currency,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new StoreEmployee
        {
            Id = r.Id,
            StoreId = r.StoreId,
            UserId = r.UserId,
            Role = r.Role,
            AddedAt = r.AddedAt,
            ScheduleStart = r.ScheduleStart,
            ScheduleEnd = r.ScheduleEnd,
            MonthlySalary = r.SalaryAmount is { } amount && IsValidCurrency(r.SalaryCurrency) ? new Money(amount, r.SalaryCurrency!) : null,
        }).ToList();
    }

    private static bool IsValidCurrency(string? currency) => !string.IsNullOrWhiteSpace(currency) && currency.Length == 3;

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

    // Same MonthlySalary trap as GetRoleAsync above (confirmed live: ComplexProperty's IsRequired
    // (false) on StoreEmployeeConfiguration alone did NOT stop EF from throwing on materialization
    // even with a fully-NULL Amount/Currency row, on this EF Core 10 preview) -- both callers
    // (GetMyStoresQueryHandler, GetUserDetailQueryHandler) are read-only and only ever need
    // StoreId/Role, so this projects everything except MonthlySalary instead of materializing the
    // full entity. Untracked by design; never route a write path through this method.
    public async Task<IReadOnlyList<StoreEmployee>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.StoreEmployees
            .Where(e => e.UserId == userId)
            .Select(e => new StoreEmployee
            {
                Id = e.Id,
                StoreId = e.StoreId,
                UserId = e.UserId,
                Role = e.Role,
                AddedAt = e.AddedAt,
                ScheduleStart = e.ScheduleStart,
                ScheduleEnd = e.ScheduleEnd,
            })
            .ToListAsync(cancellationToken);

    public void Add(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Add(storeEmployee);

    public void Remove(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Remove(storeEmployee);
}

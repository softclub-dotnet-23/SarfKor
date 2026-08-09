using Application.Abstractions;
using Domain.Stores;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreEmployeeRepository(AppDbContext dbContext) : IStoreEmployeeRepository
{
    // Every read in this repository goes through this shape (or GetRoleAsync's even narrower one) --
    // MonthlySalary is a nullable Money complex property, and EF's complex-type materialization
    // throws (ArgumentException: "Currency must be a 3-letter code") reconstructing Money for any row
    // where the salary was never set. Confirmed live: StoreEmployeeConfiguration's
    // ComplexProperty(...).IsRequired(false) does NOT stop a fresh INSERT (e.g. CreateCashierAccount
    // CommandHandler) from persisting Amount=0/Currency=NULL instead of a clean all-NULL row on this
    // EF Core 10 preview -- so this projects the two raw MonthlySalary_Amount/_Currency columns as
    // plain nullable scalars (a leaf-scalar Select translates straight to a column read, never
    // invoking Money's constructor mid-query) and reconstructs Money afterward in C#, where an
    // invalid/missing Currency is deliberately "no salary" instead of a crash. The returned entity is
    // intentionally untracked (a Select projection always is) -- a caller that needs to persist
    // changes must go through Update() below, not rely on ambient change tracking.
    private record StoreEmployeeRow(
        int Id, int StoreId, string UserId, StoreEmployeeRole Role, DateTimeOffset AddedAt,
        TimeOnly? ScheduleStart, TimeOnly? ScheduleEnd, string? FirstName, string? LastName,
        string? PhoneNumber, bool IsActive, decimal? SalaryAmount, string? SalaryCurrency);

    private static StoreEmployee ToEntity(StoreEmployeeRow r) => new()
    {
        Id = r.Id,
        StoreId = r.StoreId,
        UserId = r.UserId,
        Role = r.Role,
        AddedAt = r.AddedAt,
        ScheduleStart = r.ScheduleStart,
        ScheduleEnd = r.ScheduleEnd,
        FirstName = r.FirstName,
        LastName = r.LastName,
        PhoneNumber = r.PhoneNumber,
        IsActive = r.IsActive,
        MonthlySalary = r.SalaryAmount is { } amount && IsValidCurrency(r.SalaryCurrency) ? new Money(amount, r.SalaryCurrency!) : null,
    };

    private static bool IsValidCurrency(string? currency) => !string.IsNullOrWhiteSpace(currency) && currency.Length == 3;

    public async Task<StoreEmployee?> GetByIdAsync(int storeEmployeeId, CancellationToken cancellationToken)
    {
        var row = await dbContext.StoreEmployees
            .Where(e => e.Id == storeEmployeeId)
            .Select(e => new StoreEmployeeRow(
                e.Id, e.StoreId, e.UserId, e.Role, e.AddedAt, e.ScheduleStart, e.ScheduleEnd,
                e.FirstName, e.LastName, e.PhoneNumber, e.IsActive,
                (decimal?)e.MonthlySalary!.Amount, e.MonthlySalary!.Currency))
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    public async Task<IReadOnlyList<StoreEmployee>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.StoreEmployees
            .Where(e => e.StoreId == storeId)
            .Select(e => new StoreEmployeeRow(
                e.Id, e.StoreId, e.UserId, e.Role, e.AddedAt, e.ScheduleStart, e.ScheduleEnd,
                e.FirstName, e.LastName, e.PhoneNumber, e.IsActive,
                (decimal?)e.MonthlySalary!.Amount, e.MonthlySalary!.Currency))
            .ToListAsync(cancellationToken);

        return rows.Select(ToEntity).ToList();
    }

    public Task<bool> IsEmployeeAsync(int storeId, string userId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees.AnyAsync(e => e.StoreId == storeId && e.UserId == userId && e.IsActive, cancellationToken);

    public Task<bool> IsEmployedAnywhereAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees.AnyAsync(e => e.UserId == userId && e.IsActive, cancellationToken);

    // Projects only Role instead of materializing the full entity -- same MonthlySalary trap as
    // above, avoided the narrow way since every caller here only ever needs the role. IsActive-gated:
    // a disabled ("отключено") employee no longer counts as having store access anywhere this is
    // used (GetMyStoresQueryHandler, StoreAccessAuthorizer.IsOwnerAsync), without deleting the row.
    public Task<StoreEmployeeRole?> GetRoleAsync(int storeId, string userId, CancellationToken cancellationToken) =>
        dbContext.StoreEmployees
            .Where(e => e.StoreId == storeId && e.UserId == userId && e.IsActive)
            .Select(e => (StoreEmployeeRole?)e.Role)
            .FirstOrDefaultAsync(cancellationToken);

    // Read-only callers (GetMyStoresQueryHandler, GetUserDetailQueryHandler) only ever need
    // StoreId/Role -- MonthlySalary is dropped entirely rather than reconstructed, since neither
    // needs it and it's one less thing that can go wrong on this specific path.
    public async Task<IReadOnlyList<StoreEmployee>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.StoreEmployees
            .Where(e => e.UserId == userId && e.IsActive)
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

    // Every field marked Modified -- safe because the only source of a StoreEmployee instance reaching
    // here is GetByIdAsync's own projection moments earlier (fully populated, just untracked), not a
    // partial DTO, so re-writing untouched fields with the same value they already had is a no-op.
    public void Update(StoreEmployee storeEmployee) => dbContext.StoreEmployees.Update(storeEmployee);
}

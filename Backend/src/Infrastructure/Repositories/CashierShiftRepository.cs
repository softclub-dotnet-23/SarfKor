using Application.Abstractions;
using Domain.Sales;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CashierShiftRepository(AppDbContext dbContext) : ICashierShiftRepository
{
    // Same MonthlySalary-shaped trap as StoreEmployeeRepository (see its own comment for the full
    // story): ExpectedCash/ClosingCash are nullable Money complex properties, both genuinely NULL
    // for any shift that hasn't been closed yet -- i.e. every currently-open shift, the common
    // case this list exists to show. EF's complex-type materialization throws reconstructing Money
    // from a NULL row on this EF Core 10 preview instead of treating the whole property as null.
    // Confirmed live: GET /api/stores/{id}/cashier-shifts 500'd on every store with an open shift.
    // Same fix: project the raw scalar columns, reconstruct Money defensively in C#. The returned
    // entity is intentionally untracked -- a caller that needs to persist changes (CloseCashierShift
    // CommandHandler) must go through Update() below, not rely on ambient change tracking.
    private record CashierShiftRow(
        int Id, int StoreId, string CashierUserId, DateTimeOffset StartedAt, DateTimeOffset? EndedAt,
        decimal OpeningCashAmount, string OpeningCashCurrency,
        decimal? ExpectedCashAmount, string? ExpectedCashCurrency,
        decimal? ClosingCashAmount, string? ClosingCashCurrency);

    private static CashierShift ToEntity(CashierShiftRow r) => new()
    {
        Id = r.Id,
        StoreId = r.StoreId,
        CashierUserId = r.CashierUserId,
        StartedAt = r.StartedAt,
        EndedAt = r.EndedAt,
        OpeningCash = new Money(r.OpeningCashAmount, r.OpeningCashCurrency),
        ExpectedCash = r.ExpectedCashAmount is { } expected && IsValidCurrency(r.ExpectedCashCurrency) ? new Money(expected, r.ExpectedCashCurrency!) : null,
        ClosingCash = r.ClosingCashAmount is { } closing && IsValidCurrency(r.ClosingCashCurrency) ? new Money(closing, r.ClosingCashCurrency!) : null,
    };

    private static bool IsValidCurrency(string? currency) => !string.IsNullOrWhiteSpace(currency) && currency.Length == 3;

    public async Task<CashierShift?> GetByIdAsync(int cashierShiftId, CancellationToken cancellationToken)
    {
        var row = await dbContext.CashierShifts
            .Where(s => s.Id == cashierShiftId)
            .Select(s => new CashierShiftRow(
                s.Id, s.StoreId, s.CashierUserId, s.StartedAt, s.EndedAt,
                s.OpeningCash.Amount, s.OpeningCash.Currency,
                (decimal?)s.ExpectedCash!.Amount, s.ExpectedCash!.Currency,
                (decimal?)s.ClosingCash!.Amount, s.ClosingCash!.Currency))
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    public async Task<IReadOnlyList<CashierShift>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.CashierShifts
            .Where(s => s.StoreId == storeId)
            .Select(s => new CashierShiftRow(
                s.Id, s.StoreId, s.CashierUserId, s.StartedAt, s.EndedAt,
                s.OpeningCash.Amount, s.OpeningCash.Currency,
                (decimal?)s.ExpectedCash!.Amount, s.ExpectedCash!.Currency,
                (decimal?)s.ClosingCash!.Amount, s.ClosingCash!.Currency))
            .ToListAsync(cancellationToken);

        return rows.Select(ToEntity).ToList();
    }

    public void Add(CashierShift cashierShift) => dbContext.CashierShifts.Add(cashierShift);

    // Explicit attach-and-mark-modified for an entity that came from GetByIdAsync's own untracked
    // projection -- CloseCashierShiftCommandHandler mutates ExpectedCash/ClosingCash/EndedAt on the
    // instance it got back and needs this before SaveChanges will see anything to persist.
    public void Update(CashierShift cashierShift) => dbContext.CashierShifts.Update(cashierShift);
}

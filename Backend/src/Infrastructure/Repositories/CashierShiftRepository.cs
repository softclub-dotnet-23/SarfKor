using Application.Abstractions;
using Domain.Sales;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CashierShiftRepository(AppDbContext dbContext) : ICashierShiftRepository
{
    public Task<CashierShift?> GetByIdAsync(int cashierShiftId, CancellationToken cancellationToken) =>
        dbContext.CashierShifts.FirstOrDefaultAsync(s => s.Id == cashierShiftId, cancellationToken);

    public async Task<IReadOnlyList<CashierShift>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        await dbContext.CashierShifts.Where(s => s.StoreId == storeId).ToListAsync(cancellationToken);

    public void Add(CashierShift cashierShift) => dbContext.CashierShifts.Add(cashierShift);
}

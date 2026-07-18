using Domain.Sales;

namespace Application.Abstractions;

public interface ICashierShiftRepository
{
    Task<CashierShift?> GetByIdAsync(int cashierShiftId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CashierShift>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(CashierShift cashierShift);
}

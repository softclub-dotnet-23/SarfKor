using Domain.Sales;

namespace Application.Abstractions;

public interface ICashierShiftRepository
{
    Task<CashierShift?> GetByIdAsync(int cashierShiftId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CashierShift>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(CashierShift cashierShift);

    /// <summary>Explicit attach-and-mark-modified for an entity that came from GetByIdAsync's own
    /// safe, untracked projection (avoids the ExpectedCash/ClosingCash complex-type materialization
    /// trap, which means it no longer returns a change-tracked instance SaveChanges would pick up
    /// on its own) — callers mutate the fields they want changed on the object GetByIdAsync gave
    /// them, then call this.</summary>
    void Update(CashierShift cashierShift);
}

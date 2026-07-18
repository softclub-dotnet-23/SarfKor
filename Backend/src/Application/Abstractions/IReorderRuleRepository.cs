using Domain.Inventory;

namespace Application.Abstractions;

public interface IReorderRuleRepository
{
    Task<IReadOnlyList<ReorderRule>> GetActiveByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(ReorderRule reorderRule);
}

using Domain.Inventory;

namespace Application.Abstractions;

public interface ICostPriceRepository
{
    Task<IReadOnlyList<CostPrice>> GetLatestForStoreAsync(int storeId, CancellationToken cancellationToken);
    void Add(CostPrice costPrice);
}

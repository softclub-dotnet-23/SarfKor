using Domain.Inventory;

namespace Application.Abstractions;

public interface IStockTransferRepository
{
    Task<StockTransfer?> GetByIdAsync(int stockTransferId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockTransfer>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(StockTransfer stockTransfer);
}

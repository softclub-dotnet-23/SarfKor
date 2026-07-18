using Domain.Inventory;

namespace Application.Abstractions;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(int purchaseOrderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PurchaseOrder>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(PurchaseOrder purchaseOrder);
}

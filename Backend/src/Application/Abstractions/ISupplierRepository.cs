using Domain.Inventory;

namespace Application.Abstractions;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken);
    Task<Supplier?> GetByIdAsync(int supplierId, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(int supplierId, CancellationToken cancellationToken);
    void Add(Supplier supplier);
    void Remove(Supplier supplier);
}

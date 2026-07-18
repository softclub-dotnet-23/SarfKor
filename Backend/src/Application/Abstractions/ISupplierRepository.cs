using Domain.Inventory;

namespace Application.Abstractions;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken);
    void Add(Supplier supplier);
}

using Domain.Stores;

namespace Application.Abstractions;

public interface IStoreEmployeeRepository
{
    Task<StoreEmployee?> GetByIdAsync(int storeEmployeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreEmployee>> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    void Add(StoreEmployee storeEmployee);
    void Remove(StoreEmployee storeEmployee);
}

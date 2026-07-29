using Domain.Stores;

namespace Application.Abstractions;

public interface IStoreRepository
{
    Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int storeId, CancellationToken cancellationToken);
    Task<Store?> GetByIdAsync(int storeId, CancellationToken cancellationToken);
    Task<bool> OwnsAnyStoreAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Store>> GetOwnedByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(Store store);
}

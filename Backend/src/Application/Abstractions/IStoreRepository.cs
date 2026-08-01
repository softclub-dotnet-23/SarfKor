using Domain.Stores;

namespace Application.Abstractions;

public interface IStoreRepository
{
    Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);

    /// <summary>Same as <see cref="GetByIdsAsync"/> but excludes Pending stores — for surfaces a
    /// consumer/anonymous caller can see, where an unapproved store must not appear.</summary>
    Task<IReadOnlyList<Store>> GetApprovedByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int storeId, CancellationToken cancellationToken);
    Task<Store?> GetByIdAsync(int storeId, CancellationToken cancellationToken);
    Task<bool> OwnsAnyStoreAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Store>> GetOwnedByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Store>> GetAllAsync(int skip, int take, CancellationToken cancellationToken);
    Task<int> CountAllAsync(CancellationToken cancellationToken);
    void Add(Store store);
}

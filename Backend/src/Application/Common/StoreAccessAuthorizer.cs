using Application.Abstractions;

namespace Application.Common;

public sealed class StoreAccessAuthorizer(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository) : IStoreAccessAuthorizer
{
    public async Task<bool> IsOwnerOrEmployeeAsync(int storeId, string userId, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
            return false;

        return store.OwnerUserId == userId
            || await storeEmployeeRepository.IsEmployeeAsync(storeId, userId, cancellationToken);
    }

    public async Task<bool> IsOwnerAsync(int storeId, string userId, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken);
        return store is not null && store.OwnerUserId == userId;
    }
}

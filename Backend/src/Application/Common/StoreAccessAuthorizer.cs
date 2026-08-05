using Application.Abstractions;
using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Common;

public sealed class StoreAccessAuthorizer(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository) : IStoreAccessAuthorizer
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

    public async Task<bool> IsOperationalAsync(int storeId, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
            return false;

        if (store.Status is not StoreStatus.Active)
            return false;

        var subscription = await storeSubscriptionRepository.GetByStoreIdAsync(storeId, cancellationToken);
        // No subscription row at all is treated as operational, not blocked — pre-dates the
        // subscription system (a store approved before this feature shipped) or a data gap; the
        // nightly lifecycle job only ever suspends a subscription it can see, never invents one.
        return subscription is null || subscription.Status is not SubscriptionStatus.Suspended;
    }
}

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
        if (store is null)
            return false;
        if (store.OwnerUserId == userId)
            return true;

        // A co-owner added via StoreEmployee (Role = Owner) -- through StaffPage's own "Пригласить
        // сотрудника" role picker or /admin/users' generalized invite -- was never recognized here:
        // this checked only the single literal Store.OwnerUserId, so every one of this method's 35
        // call sites (reports, reorder alerts, promotions, purchase orders, staff management, ...)
        // 403'd a legitimately-invited co-owner. Confirmed live via this session's StorePartner
        // invite-acceptance verification. GetRoleAsync is the same MonthlySalary-avoiding projection
        // StoreEmployeeRepository already uses elsewhere, not a fresh full-entity read.
        var role = await storeEmployeeRepository.GetRoleAsync(storeId, userId, cancellationToken);
        return role == StoreEmployeeRole.Owner;
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

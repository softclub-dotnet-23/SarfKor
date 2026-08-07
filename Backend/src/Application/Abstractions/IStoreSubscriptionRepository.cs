using Domain.Subscriptions;

namespace Application.Abstractions;

public interface IStoreSubscriptionRepository
{
    Task<StoreSubscription?> GetByIdAsync(int storeSubscriptionId, CancellationToken cancellationToken);
    Task<StoreSubscription?> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreSubscription>> GetByStoreIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoreSubscription>> GetAllAsync(
        int skip, int take, SubscriptionStatus? status, int? subscriptionPlanId, IReadOnlyCollection<int>? storeIds, CancellationToken cancellationToken);

    Task<int> CountAllAsync(SubscriptionStatus? status, int? subscriptionPlanId, IReadOnlyCollection<int>? storeIds, CancellationToken cancellationToken);

    /// <summary>Trial/Active subscriptions whose current period ends before <paramref name="until"/>
    /// — backs the "истекают в ближайшие 7 дней" admin list and dashboard block.</summary>
    Task<IReadOnlyList<StoreSubscription>> GetEndingBeforeAsync(DateTimeOffset until, CancellationToken cancellationToken);

    /// <summary>PastDue subscriptions — backs the "просрочены" admin list and dashboard block.</summary>
    Task<IReadOnlyList<StoreSubscription>> GetPastDueAsync(CancellationToken cancellationToken);

    /// <summary>PastDue rows whose original period end is older than <paramref name="cutoff"/>
    /// (now minus the grace period) — the second half of the nightly lifecycle job
    /// (PastDue → Suspended). The first half reuses <see cref="GetEndingBeforeAsync"/> with
    /// cutoff=now for Trial/Active → PastDue.</summary>
    Task<IReadOnlyList<StoreSubscription>> GetPastDueOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);

    void Add(StoreSubscription subscription);
}

using Application.Abstractions;
using Domain.Subscriptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreSubscriptionRepository(AppDbContext dbContext) : IStoreSubscriptionRepository
{
    public Task<StoreSubscription?> GetByIdAsync(int storeSubscriptionId, CancellationToken cancellationToken) =>
        dbContext.StoreSubscriptions.FirstOrDefaultAsync(s => s.Id == storeSubscriptionId, cancellationToken);

    public Task<StoreSubscription?> GetByStoreIdAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.StoreSubscriptions.FirstOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);

    public async Task<IReadOnlyList<StoreSubscription>> GetByStoreIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken) =>
        await dbContext.StoreSubscriptions.Where(s => storeIds.Contains(s.StoreId)).ToListAsync(cancellationToken);

    private IQueryable<StoreSubscription> ApplyFilter(SubscriptionStatus? status, int? subscriptionPlanId, IReadOnlyCollection<int>? storeIds)
    {
        var query = dbContext.StoreSubscriptions.AsQueryable();
        if (status is not null) query = query.Where(s => s.Status == status);
        if (subscriptionPlanId is not null) query = query.Where(s => s.SubscriptionPlanId == subscriptionPlanId);
        if (storeIds is not null) query = query.Where(s => storeIds.Contains(s.StoreId));
        return query;
    }

    public async Task<IReadOnlyList<StoreSubscription>> GetAllAsync(
        int skip, int take, SubscriptionStatus? status, int? subscriptionPlanId, IReadOnlyCollection<int>? storeIds, CancellationToken cancellationToken) =>
        await ApplyFilter(status, subscriptionPlanId, storeIds).OrderBy(s => s.CurrentPeriodEndsAt).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAllAsync(SubscriptionStatus? status, int? subscriptionPlanId, IReadOnlyCollection<int>? storeIds, CancellationToken cancellationToken) =>
        ApplyFilter(status, subscriptionPlanId, storeIds).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreSubscription>> GetEndingBeforeAsync(DateTimeOffset until, CancellationToken cancellationToken) =>
        await dbContext.StoreSubscriptions
            .Where(s => (s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active) && s.CurrentPeriodEndsAt < until)
            .OrderBy(s => s.CurrentPeriodEndsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreSubscription>> GetPastDueAsync(CancellationToken cancellationToken) =>
        await dbContext.StoreSubscriptions
            .Where(s => s.Status == SubscriptionStatus.PastDue)
            .OrderBy(s => s.CurrentPeriodEndsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreSubscription>> GetPastDueOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        await dbContext.StoreSubscriptions
            .Where(s => s.Status == SubscriptionStatus.PastDue && s.CurrentPeriodEndsAt < cutoff)
            .ToListAsync(cancellationToken);

    public void Add(StoreSubscription subscription) => dbContext.StoreSubscriptions.Add(subscription);
}

using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStores;

public sealed class GetStoresQueryHandler(
    IStoreRepository storeRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IAuthService authService) : IQueryHandler<GetStoresQuery, GetStoresResult>
{
    public async Task<GetStoresResult> Handle(GetStoresQuery query, CancellationToken cancellationToken)
    {
        var filter = new StoreFilter(
            query.Status, query.SubscriptionStatus, query.ConnectedFrom, query.ConnectedTo, query.Search, query.SortBy, query.SortDescending);
        var stores = await storeRepository.GetFilteredAsync(query.Skip, query.Take, filter, cancellationToken);
        var totalCount = await storeRepository.CountFilteredAsync(filter, cancellationToken);

        var storeIds = stores.Select(s => s.Id).ToList();
        var subscriptions = (await storeSubscriptionRepository.GetByStoreIdsAsync(storeIds, cancellationToken))
            .ToDictionary(s => s.StoreId);

        var planIds = subscriptions.Values.Select(s => s.SubscriptionPlanId).Distinct().ToList();
        var plans = new Dictionary<int, string>();
        foreach (var planId in planIds)
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is not null) plans[planId] = plan.Name;
        }

        var ownerIds = stores.Select(s => s.OwnerUserId).Distinct().ToList();
        var emailsByOwnerId = await authService.GetEmailsByUserIdsAsync(ownerIds, cancellationToken);

        var dtos = stores.Select(s =>
        {
            subscriptions.TryGetValue(s.Id, out var sub);
            return new AdminStoreListItemDto(
                s.Id,
                s.Name,
                s.Address,
                s.Status,
                s.OwnerUserId,
                emailsByOwnerId.GetValueOrDefault(s.OwnerUserId),
                sub?.Status,
                sub is not null ? plans.GetValueOrDefault(sub.SubscriptionPlanId) : null,
                sub?.CurrentPeriodEndsAt);
        }).ToList();

        return new GetStoresResult(dtos, totalCount);
    }
}

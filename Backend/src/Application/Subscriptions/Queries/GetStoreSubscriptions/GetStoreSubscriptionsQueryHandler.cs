using Application.Abstractions;
using Application.Common;

namespace Application.Subscriptions.Queries.GetStoreSubscriptions;

public sealed class GetStoreSubscriptionsQueryHandler(
    IStoreSubscriptionRepository storeSubscriptionRepository,
    IStoreRepository storeRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository) : IQueryHandler<GetStoreSubscriptionsQuery, GetStoreSubscriptionsResult>
{
    public async Task<GetStoreSubscriptionsResult> Handle(GetStoreSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<int>? storeIds = null;
        if (!string.IsNullOrWhiteSpace(query.StoreSearch))
        {
            var matches = await storeRepository.GetFilteredAsync(
                0, 500, new StoreFilter(null, null, null, null, query.StoreSearch, null, false), cancellationToken);
            storeIds = matches.Select(s => s.Id).ToList();
        }

        var subscriptions = await storeSubscriptionRepository.GetAllAsync(
            query.Skip, query.Take, query.Status, query.SubscriptionPlanId, storeIds, cancellationToken);
        var totalCount = await storeSubscriptionRepository.CountAllAsync(query.Status, query.SubscriptionPlanId, storeIds, cancellationToken);

        var storesById = (await storeRepository.GetByIdsAsync(subscriptions.Select(s => s.StoreId).Distinct().ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        var plansById = new Dictionary<int, string>();
        foreach (var planId in subscriptions.Select(s => s.SubscriptionPlanId).Distinct())
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is not null) plansById[planId] = plan.Name;
        }

        var dtos = subscriptions.Select(s => new StoreSubscriptionListItemDto(
            s.Id,
            s.StoreId,
            storesById.GetValueOrDefault(s.StoreId)?.Name ?? $"Store #{s.StoreId}",
            s.SubscriptionPlanId,
            plansById.GetValueOrDefault(s.SubscriptionPlanId, "—"),
            s.Status,
            s.StartedAt,
            s.CurrentPeriodEndsAt,
            s.PriceAtIssue.Amount,
            s.PriceAtIssue.Currency)).ToList();

        return new GetStoreSubscriptionsResult(dtos, totalCount);
    }
}

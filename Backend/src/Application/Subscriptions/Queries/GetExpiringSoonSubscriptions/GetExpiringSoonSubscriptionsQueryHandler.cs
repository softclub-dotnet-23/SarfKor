using Application.Abstractions;
using Application.Common;

namespace Application.Subscriptions.Queries.GetExpiringSoonSubscriptions;

public sealed class GetExpiringSoonSubscriptionsQueryHandler(
    IStoreSubscriptionRepository storeSubscriptionRepository,
    IStoreRepository storeRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository) : IQueryHandler<GetExpiringSoonSubscriptionsQuery, GetExpiringSoonSubscriptionsResult>
{
    public async Task<GetExpiringSoonSubscriptionsResult> Handle(GetExpiringSoonSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        var subscriptions = await storeSubscriptionRepository.GetEndingBeforeAsync(
            DateTimeOffset.UtcNow.AddDays(query.WithinDays), cancellationToken);

        var storesById = (await storeRepository.GetByIdsAsync(subscriptions.Select(s => s.StoreId).ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        var plansById = new Dictionary<int, string>();
        foreach (var planId in subscriptions.Select(s => s.SubscriptionPlanId).Distinct())
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is not null) plansById[planId] = plan.Name;
        }

        var dtos = subscriptions.Select(s => new ExpiringSubscriptionDto(
            s.Id,
            s.StoreId,
            storesById.GetValueOrDefault(s.StoreId)?.Name ?? $"Store #{s.StoreId}",
            plansById.GetValueOrDefault(s.SubscriptionPlanId, "—"),
            s.CurrentPeriodEndsAt)).ToList();

        return new GetExpiringSoonSubscriptionsResult(dtos);
    }
}

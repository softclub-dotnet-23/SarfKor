using Application.Abstractions;
using Application.Common;

namespace Application.Subscriptions.Queries.GetPastDueSubscriptions;

public sealed class GetPastDueSubscriptionsQueryHandler(
    IStoreSubscriptionRepository storeSubscriptionRepository,
    IStoreRepository storeRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository) : IQueryHandler<GetPastDueSubscriptionsQuery, GetPastDueSubscriptionsResult>
{
    public async Task<GetPastDueSubscriptionsResult> Handle(GetPastDueSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        var subscriptions = await storeSubscriptionRepository.GetPastDueAsync(cancellationToken);

        var storesById = (await storeRepository.GetByIdsAsync(subscriptions.Select(s => s.StoreId).ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        var plansById = new Dictionary<int, string>();
        foreach (var planId in subscriptions.Select(s => s.SubscriptionPlanId).Distinct())
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is not null) plansById[planId] = plan.Name;
        }

        var dtos = subscriptions.Select(s => new PastDueSubscriptionDto(
            s.Id,
            s.StoreId,
            storesById.GetValueOrDefault(s.StoreId)?.Name ?? $"Store #{s.StoreId}",
            plansById.GetValueOrDefault(s.SubscriptionPlanId, "—"),
            s.CurrentPeriodEndsAt)).ToList();

        return new GetPastDueSubscriptionsResult(dtos);
    }
}

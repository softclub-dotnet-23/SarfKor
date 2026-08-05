using Domain.Subscriptions;

namespace Application.Subscriptions.Queries.GetStoreSubscriptions;

public sealed record GetStoreSubscriptionsQuery(int Skip, int Take, SubscriptionStatus? Status, int? SubscriptionPlanId, string? StoreSearch);

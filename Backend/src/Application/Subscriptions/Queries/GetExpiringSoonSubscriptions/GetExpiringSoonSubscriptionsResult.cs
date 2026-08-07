namespace Application.Subscriptions.Queries.GetExpiringSoonSubscriptions;

public sealed record ExpiringSubscriptionDto(
    int StoreSubscriptionId,
    int StoreId,
    string StoreName,
    string SubscriptionPlanName,
    DateTimeOffset CurrentPeriodEndsAt);

public sealed record GetExpiringSoonSubscriptionsResult(IReadOnlyList<ExpiringSubscriptionDto> Subscriptions);

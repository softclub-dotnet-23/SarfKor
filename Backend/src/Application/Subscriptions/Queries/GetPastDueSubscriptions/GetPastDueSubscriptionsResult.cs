namespace Application.Subscriptions.Queries.GetPastDueSubscriptions;

public sealed record PastDueSubscriptionDto(
    int StoreSubscriptionId,
    int StoreId,
    string StoreName,
    string SubscriptionPlanName,
    DateTimeOffset CurrentPeriodEndsAt);

public sealed record GetPastDueSubscriptionsResult(IReadOnlyList<PastDueSubscriptionDto> Subscriptions);

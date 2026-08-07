using Domain.Subscriptions;

namespace Application.Subscriptions.Queries.GetStoreSubscriptions;

public sealed record StoreSubscriptionListItemDto(
    int StoreSubscriptionId,
    int StoreId,
    string StoreName,
    int SubscriptionPlanId,
    string SubscriptionPlanName,
    SubscriptionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset CurrentPeriodEndsAt,
    decimal PriceAtIssueAmount,
    string PriceAtIssueCurrency);

public sealed record GetStoreSubscriptionsResult(IReadOnlyList<StoreSubscriptionListItemDto> Subscriptions, int TotalCount);

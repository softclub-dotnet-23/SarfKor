using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Stores.Queries.GetStoreDetail;

public enum GetStoreDetailOutcome
{
    Found,
    NotFound
}

public sealed record AdminStoreSubscriptionDto(
    int StoreSubscriptionId,
    int SubscriptionPlanId,
    string SubscriptionPlanName,
    SubscriptionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset CurrentPeriodEndsAt,
    decimal PriceAtIssueAmount,
    string PriceAtIssueCurrency,
    string? Note);

public sealed record GetStoreDetailResult(
    GetStoreDetailOutcome Outcome,
    int StoreId,
    string? Name,
    string? Address,
    double? Latitude,
    double? Longitude,
    StoreStatus? Status,
    string? StatusReason,
    DateTimeOffset? StatusChangedAt,
    string? OwnerUserId,
    string? OwnerEmail,
    bool? IsVatPayer,
    StoreTaxRegime? TaxRegime,
    AdminStoreSubscriptionDto? Subscription);

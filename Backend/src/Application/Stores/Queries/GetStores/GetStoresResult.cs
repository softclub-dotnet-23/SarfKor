using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Stores.Queries.GetStores;

public sealed record AdminStoreListItemDto(
    int StoreId,
    string Name,
    string Address,
    StoreStatus Status,
    string OwnerUserId,
    string? OwnerEmail,
    SubscriptionStatus? SubscriptionStatus,
    string? SubscriptionPlanName,
    DateTimeOffset? SubscriptionCurrentPeriodEndsAt);

public sealed record GetStoresResult(IReadOnlyList<AdminStoreListItemDto> Stores, int TotalCount);

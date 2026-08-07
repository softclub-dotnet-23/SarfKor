using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Stores.Queries.GetStores;

public sealed record GetStoresQuery(
    int Skip,
    int Take,
    StoreStatus? Status,
    SubscriptionStatus? SubscriptionStatus,
    DateTimeOffset? ConnectedFrom,
    DateTimeOffset? ConnectedTo,
    string? Search,
    string? SortBy,
    bool SortDescending);

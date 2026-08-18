using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Abstractions;

// City isn't a queryable filter -- Store only has a free-text Address (no structured City column,
// see Store.cs), so "search by city" would need a schema change/geocoding step that's out of scope
// here; free-text Search already matches against Address. SubscriptionStatus/ConnectedFrom/
// ConnectedTo cover the rest of ADMIN_PROMPT.md §2.2's filter list.
public sealed record StoreFilter(
    StoreStatus? Status,
    SubscriptionStatus? SubscriptionStatus,
    DateTimeOffset? ConnectedFrom,
    DateTimeOffset? ConnectedTo,
    string? Search,
    string? SortBy,
    bool SortDescending);

public interface IStoreRepository
{
    Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);

    /// <summary>Same as <see cref="GetByIdsAsync"/> but excludes stores not currently Active — for
    /// surfaces a consumer/anonymous caller can see, where a pending/suspended/blocked/archived
    /// store must not appear (their public price data stays visible per ADMIN_PROMPT.md §2.1's
    /// Suspended carve-out — this exclusion is specifically for admin-side non-Active states like
    /// PendingApproval, not a blanket "hide anything not paying").</summary>
    Task<IReadOnlyList<Store>> GetApprovedByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int storeId, CancellationToken cancellationToken);
    Task<Store?> GetByIdAsync(int storeId, CancellationToken cancellationToken);
    Task<bool> OwnsAnyStoreAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Store>> GetOwnedByUserIdAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Case-insensitive substring match on Name/Address, scoped to stores this one user
    /// owns (never another owner's) -- backs a searchable store picker (e.g. "which of my other
    /// stores is this stock transfer going to") without ever surfacing a raw store id for someone
    /// to type in by hand. Exact name match ranked first, same convention as
    /// ProductRepository.SearchAsync's exact-barcode ranking.</summary>
    Task<(IReadOnlyList<Store> Items, int TotalCount)> SearchOwnedByUserIdAsync(
        string userId, string? search, int skip, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<Store>> GetAllAsync(int skip, int take, CancellationToken cancellationToken);
    Task<int> CountAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Store>> GetFilteredAsync(int skip, int take, StoreFilter filter, CancellationToken cancellationToken);
    Task<int> CountFilteredAsync(StoreFilter filter, CancellationToken cancellationToken);

    /// <summary>Counts by Status — the "Магазины по статусам" metrics tile (§2.5).</summary>
    Task<IReadOnlyDictionary<StoreStatus, int>> CountByStatusAsync(CancellationToken cancellationToken);

    /// <summary>Bare ConnectedAt timestamps in range — the "подключений" half of the metrics time
    /// series (grouped by day in the query handler).</summary>
    Task<IReadOnlyList<DateTimeOffset>> GetConnectedAtInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    void Add(Store store);
}

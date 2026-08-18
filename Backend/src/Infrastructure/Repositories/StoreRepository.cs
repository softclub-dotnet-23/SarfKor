using Application.Abstractions;
using Domain.Stores;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class StoreRepository(AppDbContext dbContext) : IStoreRepository
{
    public async Task<IReadOnlyList<Store>> GetByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => storeIds.Contains(s.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Store>> GetApprovedByIdsAsync(IReadOnlyCollection<int> storeIds, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => storeIds.Contains(s.Id) && s.Status == StoreStatus.Active).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.AnyAsync(s => s.Id == storeId, cancellationToken);

    public Task<Store?> GetByIdAsync(int storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);

    public Task<bool> OwnsAnyStoreAsync(string userId, CancellationToken cancellationToken) =>
        dbContext.Stores.AnyAsync(s => s.OwnerUserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Store>> GetOwnedByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.Stores.Where(s => s.OwnerUserId == userId).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Store> Items, int TotalCount)> SearchOwnedByUserIdAsync(
        string userId, string? search, int skip, int take, CancellationToken cancellationToken)
    {
        // OwnerUserId already has an index from the FK relationship (StoreConfiguration), so this
        // starts from a handful of rows at most for any real owner -- a leading-wildcard ILIKE over
        // Name/Address doesn't need (and a B-tree index couldn't accelerate) a dedicated index on
        // top of that, unlike ProductRepository.SearchAsync's platform-wide search.
        var query = dbContext.Stores.Where(s => s.OwnerUserId == userId);

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, $"%{term}%") ||
                EF.Functions.ILike(s.Address, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => term != null && s.Name == term)
            .ThenBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Store>> GetAllAsync(int skip, int take, CancellationToken cancellationToken) =>
        await dbContext.Stores.OrderBy(s => s.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAllAsync(CancellationToken cancellationToken) =>
        dbContext.Stores.CountAsync(cancellationToken);

    private IQueryable<Store> ApplyFilter(StoreFilter filter)
    {
        var query = dbContext.Stores.AsQueryable();

        if (filter.Status is { } status)
            query = query.Where(s => s.Status == status);

        if (filter.SubscriptionStatus is { } subStatus)
        {
            var storeIdsWithStatus = dbContext.StoreSubscriptions.Where(sub => sub.Status == subStatus).Select(sub => sub.StoreId);
            query = query.Where(s => storeIdsWithStatus.Contains(s.Id));
        }

        if (filter.ConnectedFrom is { } connectedFrom)
            query = query.Where(s => s.ConnectedAt >= connectedFrom);

        if (filter.ConnectedTo is { } connectedTo)
            query = query.Where(s => s.ConnectedAt < connectedTo);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            // AppDbContext is an IdentityDbContext<ApplicationUser>, so Users is queryable from the
            // same context -- "search by name or owner" (ADMIN_PROMPT.md §2.2) needs a match against
            // the owner's email too, not just the store's own Name/Address.
            var matchingOwnerIds = dbContext.Users.Where(u => EF.Functions.ILike(u.Email!, term)).Select(u => u.Id);
            query = query.Where(s => EF.Functions.ILike(s.Name, term) || EF.Functions.ILike(s.Address, term) || matchingOwnerIds.Contains(s.OwnerUserId));
        }

        query = (filter.SortBy, filter.SortDescending) switch
        {
            ("name", false) => query.OrderBy(s => s.Name),
            ("name", true) => query.OrderByDescending(s => s.Name),
            ("status", false) => query.OrderBy(s => s.Status),
            ("status", true) => query.OrderByDescending(s => s.Status),
            (_, true) => query.OrderByDescending(s => s.Id),
            _ => query.OrderBy(s => s.Id)
        };

        return query;
    }

    public async Task<IReadOnlyList<Store>> GetFilteredAsync(int skip, int take, StoreFilter filter, CancellationToken cancellationToken) =>
        await ApplyFilter(filter).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountFilteredAsync(StoreFilter filter, CancellationToken cancellationToken) =>
        ApplyFilter(filter).CountAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<StoreStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Stores
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Status, r => r.Count);
    }

    public async Task<IReadOnlyList<DateTimeOffset>> GetConnectedAtInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        await dbContext.Stores
            .Where(s => s.ConnectedAt >= from && s.ConnectedAt < to)
            .Select(s => s.ConnectedAt)
            .ToListAsync(cancellationToken);

    public void Add(Store store) => dbContext.Stores.Add(store);
}

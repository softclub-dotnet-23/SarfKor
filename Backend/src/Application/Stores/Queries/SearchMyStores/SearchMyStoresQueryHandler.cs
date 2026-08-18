using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.SearchMyStores;

/// <summary>
/// Backs the store picker used wherever the caller needs to name one of their OWN stores (e.g.
/// "which of your other stores is this stock transfer going to") -- deliberately owner-only, not
/// GetMyStoresQuery's owned+employed union, since employment at a store never implies the right to
/// move that store's inventory around. Self-scoped by construction (search only ever runs against
/// this UserId's own rows), so there is no Forbidden/NotFound outcome to model.
/// </summary>
public sealed class SearchMyStoresQueryHandler(
    IStoreRepository storeRepository) : IQueryHandler<SearchMyStoresQuery, SearchMyStoresResult>
{
    public async Task<SearchMyStoresResult> Handle(SearchMyStoresQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await storeRepository.SearchOwnedByUserIdAsync(
            query.UserId, query.Search, query.Skip, query.Take, cancellationToken);

        var dtos = items.Select(s => new SearchMyStoreItemDto(s.Id, s.Name, s.Address)).ToList();
        return new SearchMyStoresResult(dtos, totalCount);
    }
}

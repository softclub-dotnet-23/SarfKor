using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreLocations;

// "Торговые точки" tab (ADMIN_PROMPT.md §3, store card) -- a Store row *is* one physical location;
// an owner with several shops has several Store rows sharing OwnerUserId. Admin-only, no ownership
// check (unlike GetStoreEmployeesQuery) since this is precisely what lets support look across an
// owner's whole footprint from any one of their stores.
public sealed class GetStoreLocationsQueryHandler(IStoreRepository storeRepository)
    : IQueryHandler<GetStoreLocationsQuery, GetStoreLocationsResult>
{
    public async Task<GetStoreLocationsResult> Handle(GetStoreLocationsQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreLocationsResult(GetStoreLocationsOutcome.StoreNotFound, null);

        var siblings = await storeRepository.GetOwnedByUserIdAsync(store.OwnerUserId, cancellationToken);
        var dtos = siblings
            .OrderBy(s => s.Id)
            .Select(s => new StoreLocationDto(s.Id, s.Name, s.Address, s.Status))
            .ToList();

        return new GetStoreLocationsResult(GetStoreLocationsOutcome.Found, dtos);
    }
}

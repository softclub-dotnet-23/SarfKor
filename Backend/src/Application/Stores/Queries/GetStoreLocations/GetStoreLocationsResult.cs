using Domain.Stores;

namespace Application.Stores.Queries.GetStoreLocations;

public sealed record StoreLocationDto(int StoreId, string Name, string Address, StoreStatus Status);

public enum GetStoreLocationsOutcome
{
    Found,
    StoreNotFound
}

public sealed record GetStoreLocationsResult(GetStoreLocationsOutcome Outcome, IReadOnlyList<StoreLocationDto>? Locations);

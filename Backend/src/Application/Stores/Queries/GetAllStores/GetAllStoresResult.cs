using Domain.Stores;

namespace Application.Stores.Queries.GetAllStores;

// OwnerEmail is nullable, not a throw: a missing/emailless Identity row must not break the whole
// page just because one store's owner account is in an odd state.
public sealed record AdminStoreDto(
    int StoreId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    StoreStatus Status,
    string OwnerUserId,
    string? OwnerEmail);

public sealed record GetAllStoresResult(IReadOnlyList<AdminStoreDto> Stores, int TotalCount);

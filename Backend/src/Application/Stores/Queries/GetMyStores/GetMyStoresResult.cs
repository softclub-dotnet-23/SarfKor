namespace Application.Stores.Queries.GetMyStores;

public sealed record MyStoreDto(int StoreId, string Name, string Role);

public sealed record GetMyStoresResult(IReadOnlyList<MyStoreDto> Stores);

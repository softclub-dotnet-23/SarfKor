namespace Application.Stores.Queries.SearchMyStores;

public sealed record SearchMyStoreItemDto(int StoreId, string Name, string Address);

public sealed record SearchMyStoresResult(IReadOnlyList<SearchMyStoreItemDto> Stores, int TotalCount);

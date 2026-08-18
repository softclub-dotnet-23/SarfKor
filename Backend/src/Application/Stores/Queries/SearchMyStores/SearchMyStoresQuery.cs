namespace Application.Stores.Queries.SearchMyStores;

public sealed record SearchMyStoresQuery(string UserId, string? Search, int Skip, int Take);

using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.CompareStoresForShoppingList;

public sealed class CompareStoresForShoppingListQueryHandler(
    IPriceEntryRepository priceEntryRepository,
    IStoreRepository storeRepository) : IQueryHandler<CompareStoresForShoppingListQuery, CompareStoresForShoppingListResult>
{
    public async Task<CompareStoresForShoppingListResult> Handle(CompareStoresForShoppingListQuery query, CancellationToken cancellationToken)
    {
        var totalByStore = new Dictionary<int, (decimal Total, int MatchedCount, string Currency)>();

        // Single batched query instead of one round-trip per product — matters once a shopping
        // list has more than a handful of items.
        var entries = await priceEntryRepository.GetLatestPerStoreForProductsAsync(query.ProductIds, cancellationToken);
        foreach (var entry in entries)
        {
            totalByStore.TryGetValue(entry.StoreId, out var current);
            totalByStore[entry.StoreId] = (current.Total + entry.Price.Amount, current.MatchedCount + 1, entry.Price.Currency);
        }

        var completeMatches = totalByStore.Where(kv => kv.Value.MatchedCount == query.ProductIds.Count).ToList();

        var stores = await storeRepository.GetByIdsAsync(completeMatches.Select(kv => kv.Key).ToList(), cancellationToken);
        var storesById = stores.ToDictionary(s => s.Id);

        var results = completeMatches
            .Where(kv => storesById.ContainsKey(kv.Key))
            .Select(kv =>
            {
                var store = storesById[kv.Key];
                double? distanceKm = query.UserLatitude.HasValue && query.UserLongitude.HasValue
                    ? GeoDistance.CalculateKm(query.UserLatitude.Value, query.UserLongitude.Value, store.Location.Latitude, store.Location.Longitude)
                    : null;

                return new StoreBasketDto(store.Id, store.Name, kv.Value.Total, kv.Value.Currency, distanceKm);
            })
            .OrderBy(s => s.TotalPrice)
            .ToList();

        return new CompareStoresForShoppingListResult(results);
    }
}

using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.ScanBarcode;

public sealed class ScanBarcodeQueryHandler(
    IProductRepository productRepository,
    IPriceEntryRepository priceEntryRepository,
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer) : IQueryHandler<ScanBarcodeQuery, ScanBarcodeResult?>
{
    public async Task<ScanBarcodeResult?> Handle(ScanBarcodeQuery query, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByBarcodeAsync(query.Barcode, cancellationToken);
        if (product is null)
            return null;

        // Unverified entries (low-trust author, not yet corroborated by a second independent
        // submission — see PriceEntry.IsVerified/TrustScoreFormula) never reach a public result;
        // that store's price simply doesn't show for this product rather than falling back to a
        // stale verified price, matching ADMIN_PROMPT.md §1 literally ("не отображается публично").
        var priceEntries = (await priceEntryRepository.GetLatestPerStoreAsync(product.Id, cancellationToken))
            .Where(p => p.IsVerified)
            .ToList();
        var stores = await storeRepository.GetApprovedByIdsAsync(priceEntries.Select(p => p.StoreId).ToList(), cancellationToken);
        var storesById = stores.ToDictionary(s => s.Id);

        // A logged-in StorePartner/cashier must always see their own store's price at their own
        // register, even before Admin has approved the store for consumer-facing search.
        if (query.CallerUserId is not null)
        {
            var unapprovedStoreIds = priceEntries.Select(p => p.StoreId).Distinct().Where(id => !storesById.ContainsKey(id));
            foreach (var storeId in unapprovedStoreIds)
            {
                if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(storeId, query.CallerUserId, cancellationToken))
                    continue;

                var ownStore = await storeRepository.GetByIdAsync(storeId, cancellationToken);
                if (ownStore is not null)
                    storesById[storeId] = ownStore;
            }
        }

        var results = priceEntries
            .Where(p => storesById.ContainsKey(p.StoreId))
            .Select(p =>
            {
                var store = storesById[p.StoreId];
                double? distanceKm = query.UserLatitude.HasValue && query.UserLongitude.HasValue
                    ? GeoDistance.CalculateKm(query.UserLatitude.Value, query.UserLongitude.Value, store.Location.Latitude, store.Location.Longitude)
                    : null;

                return new StorePriceDto(store.Id, store.Name, p.Price.Amount, p.Price.Currency, distanceKm, p.Id);
            })
            .OrderBy(s => s.DistanceKm ?? double.MaxValue)
            .ThenBy(s => s.Price)
            .ToList();

        return new ScanBarcodeResult(product.Id, product.Name, results);
    }
}

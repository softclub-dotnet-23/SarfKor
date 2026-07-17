using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.ScanBarcode;

public sealed class ScanBarcodeQueryHandler(
    IProductRepository productRepository,
    IPriceEntryRepository priceEntryRepository,
    IStoreRepository storeRepository) : IQueryHandler<ScanBarcodeQuery, ScanBarcodeResult?>
{
    public async Task<ScanBarcodeResult?> Handle(ScanBarcodeQuery query, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByBarcodeAsync(query.Barcode, cancellationToken);
        if (product is null)
            return null;

        var priceEntries = await priceEntryRepository.GetLatestPerStoreAsync(product.Id, cancellationToken);
        var stores = await storeRepository.GetByIdsAsync(priceEntries.Select(p => p.StoreId).ToList(), cancellationToken);
        var storesById = stores.ToDictionary(s => s.Id);

        var results = priceEntries
            .Where(p => storesById.ContainsKey(p.StoreId))
            .Select(p =>
            {
                var store = storesById[p.StoreId];
                double? distanceKm = query.UserLatitude.HasValue && query.UserLongitude.HasValue
                    ? GeoDistance.CalculateKm(query.UserLatitude.Value, query.UserLongitude.Value, store.Location.Latitude, store.Location.Longitude)
                    : null;

                return new StorePriceDto(store.Id, store.Name, p.Price.Amount, p.Price.Currency, distanceKm);
            })
            .OrderBy(s => s.DistanceKm ?? double.MaxValue)
            .ThenBy(s => s.Price)
            .ToList();

        return new ScanBarcodeResult(product.Id, product.Name, results);
    }
}

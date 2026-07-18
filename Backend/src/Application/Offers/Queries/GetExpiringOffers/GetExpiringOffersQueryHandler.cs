using Application.Abstractions;
using Application.Common;

namespace Application.Offers.Queries.GetExpiringOffers;

public sealed class GetExpiringOffersQueryHandler(
    IExpiringOfferRepository expiringOfferRepository,
    IProductRepository productRepository,
    IStoreRepository storeRepository) : IQueryHandler<GetExpiringOffersQuery, GetExpiringOffersResult>
{
    public async Task<GetExpiringOffersResult> Handle(GetExpiringOffersQuery query, CancellationToken cancellationToken)
    {
        var offers = await expiringOfferRepository.GetActiveAsync(query.StoreId, DateTimeOffset.UtcNow, cancellationToken);

        var products = await productRepository.GetByIdsAsync(offers.Select(o => o.ProductId).Distinct().ToList(), cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        var stores = await storeRepository.GetByIdsAsync(offers.Select(o => o.StoreId).Distinct().ToList(), cancellationToken);
        var storesById = stores.ToDictionary(s => s.Id);

        var dtos = offers
            .Where(o => productsById.ContainsKey(o.ProductId) && storesById.ContainsKey(o.StoreId))
            .Select(o =>
            {
                var store = storesById[o.StoreId];
                double? distanceKm = query.UserLatitude.HasValue && query.UserLongitude.HasValue
                    ? GeoDistance.CalculateKm(query.UserLatitude.Value, query.UserLongitude.Value, store.Location.Latitude, store.Location.Longitude)
                    : null;

                return new ExpiringOfferDto(
                    o.Id,
                    o.ProductId,
                    productsById[o.ProductId].Name,
                    o.StoreId,
                    store.Name,
                    o.OriginalPrice.Amount,
                    o.DiscountedPrice.Amount,
                    o.DiscountedPrice.Currency,
                    o.ExpiresAt,
                    distanceKm);
            })
            .OrderBy(o => o.DistanceKm ?? double.MaxValue)
            .ThenBy(o => o.ExpiresAt)
            .ToList();

        return new GetExpiringOffersResult(dtos);
    }
}

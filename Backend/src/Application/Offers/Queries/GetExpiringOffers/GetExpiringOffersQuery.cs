namespace Application.Offers.Queries.GetExpiringOffers;

public sealed record GetExpiringOffersQuery(int? StoreId, double? UserLatitude, double? UserLongitude);

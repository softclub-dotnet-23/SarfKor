namespace Application.Offers.Queries.GetExpiringOffers;

public sealed record ExpiringOfferDto(
    int OfferId,
    int ProductId,
    string ProductName,
    int StoreId,
    string StoreName,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    string Currency,
    DateTimeOffset ExpiresAt,
    double? DistanceKm);

public sealed record GetExpiringOffersResult(IReadOnlyList<ExpiringOfferDto> Offers);

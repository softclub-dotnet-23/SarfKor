namespace Application.Offers.Commands.PublishExpiringOffer;

public sealed record PublishExpiringOfferCommand(
    int StoreId,
    int ProductId,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    string Currency,
    DateTimeOffset ExpiresAt,
    string PerformedByUserId);

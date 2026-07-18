namespace Application.Offers.Commands.PublishExpiringOffer;

public enum PublishExpiringOfferOutcome
{
    Published,
    StoreNotFound,
    ProductNotFound,
    Forbidden
}

public sealed record PublishExpiringOfferResult(PublishExpiringOfferOutcome Outcome, int? OfferId);

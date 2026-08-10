namespace Application.Offers.Commands.PublishExpiringOffer;

public enum PublishExpiringOfferOutcome
{
    Published,
    StoreNotFound,
    ProductNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record PublishExpiringOfferResult(PublishExpiringOfferOutcome Outcome, int? OfferId);

namespace Application.Pricing.Commands.SubmitPriceUpdate;

public enum SubmitPriceUpdateOutcome
{
    Submitted,
    ProductNotFound,
    StoreNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record SubmitPriceUpdateResult(SubmitPriceUpdateOutcome Outcome, int? PriceEntryId, DateTimeOffset? RecordedAt);

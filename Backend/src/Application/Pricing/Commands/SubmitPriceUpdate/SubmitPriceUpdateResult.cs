namespace Application.Pricing.Commands.SubmitPriceUpdate;

public sealed record SubmitPriceUpdateResult(int PriceEntryId, DateTimeOffset RecordedAt);

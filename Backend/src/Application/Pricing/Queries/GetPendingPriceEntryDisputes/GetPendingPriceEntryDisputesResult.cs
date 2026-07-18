namespace Application.Pricing.Queries.GetPendingPriceEntryDisputes;

public sealed record PriceEntryDisputeDto(int DisputeId, int PriceEntryId, string DisputedByUserId, string Reason, DateTimeOffset CreatedAt);

public sealed record GetPendingPriceEntryDisputesResult(IReadOnlyList<PriceEntryDisputeDto> Disputes);

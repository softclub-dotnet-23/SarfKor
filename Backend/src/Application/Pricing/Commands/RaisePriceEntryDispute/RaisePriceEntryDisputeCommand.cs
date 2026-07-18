namespace Application.Pricing.Commands.RaisePriceEntryDispute;

public sealed record RaisePriceEntryDisputeCommand(int PriceEntryId, string DisputedByUserId, string Reason);

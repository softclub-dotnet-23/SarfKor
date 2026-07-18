namespace Application.Pricing.Commands.ResolvePriceEntryDispute;

public sealed record ResolvePriceEntryDisputeCommand(int DisputeId, bool Uphold, string AdminUserId);

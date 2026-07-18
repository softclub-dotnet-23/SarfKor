namespace Application.Pricing.Commands.ResolvePriceEntryDispute;

public enum ResolvePriceEntryDisputeOutcome
{
    Upheld,
    Dismissed,
    NotFound,
    AlreadyResolved
}

public sealed record ResolvePriceEntryDisputeResult(ResolvePriceEntryDisputeOutcome Outcome);

namespace Application.Pricing.Commands.RaisePriceEntryDispute;

public enum RaisePriceEntryDisputeOutcome
{
    Raised,
    PriceEntryNotFound
}

public sealed record RaisePriceEntryDisputeResult(RaisePriceEntryDisputeOutcome Outcome, int? DisputeId);

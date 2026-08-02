namespace Application.Pricing.Commands.RaisePriceEntryDispute;

public enum RaisePriceEntryDisputeOutcome
{
    Raised,
    PriceEntryNotFound,
    AlreadyDisputed
}

public sealed record RaisePriceEntryDisputeResult(RaisePriceEntryDisputeOutcome Outcome, int? DisputeId);

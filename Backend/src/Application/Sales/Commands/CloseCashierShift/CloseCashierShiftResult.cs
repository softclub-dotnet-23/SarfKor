namespace Application.Sales.Commands.CloseCashierShift;

public enum CloseCashierShiftOutcome
{
    Closed,
    NotFound,
    Forbidden,
    AlreadyClosed,
    SubscriptionInactive
}

public sealed record CloseCashierShiftResult(CloseCashierShiftOutcome Outcome, decimal? ExpectedCash, decimal? ClosingCash, decimal? Discrepancy);

namespace Application.Sales.Commands.RecordCommission;

public enum RecordCommissionOutcome
{
    Recorded,
    SaleNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record RecordCommissionResult(RecordCommissionOutcome Outcome, int? CommissionId);

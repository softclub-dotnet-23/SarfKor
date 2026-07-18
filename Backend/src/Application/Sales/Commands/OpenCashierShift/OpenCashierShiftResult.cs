namespace Application.Sales.Commands.OpenCashierShift;

public enum OpenCashierShiftOutcome
{
    Opened,
    StoreNotFound,
    Forbidden
}

public sealed record OpenCashierShiftResult(OpenCashierShiftOutcome Outcome, int? CashierShiftId);

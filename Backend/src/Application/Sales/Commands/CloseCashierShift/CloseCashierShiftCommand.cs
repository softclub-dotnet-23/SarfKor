namespace Application.Sales.Commands.CloseCashierShift;

public sealed record CloseCashierShiftCommand(int CashierShiftId, decimal ClosingCash, string PerformedByUserId);

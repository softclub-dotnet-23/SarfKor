namespace Application.Sales.Commands.OpenCashierShift;

public sealed record OpenCashierShiftCommand(int StoreId, decimal OpeningCash, string Currency, string PerformedByUserId);

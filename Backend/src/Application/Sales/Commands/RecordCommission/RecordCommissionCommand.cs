namespace Application.Sales.Commands.RecordCommission;

public sealed record RecordCommissionCommand(int SaleTransactionId, decimal Amount, string Currency, string PerformedByUserId);

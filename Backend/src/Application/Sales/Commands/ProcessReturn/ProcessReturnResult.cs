namespace Application.Sales.Commands.ProcessReturn;

public enum ProcessReturnOutcome
{
    Processed,
    SaleNotFound,
    Forbidden,
    SaleNotCompleted,
    LineNotFound,
    ExceedsAvailableQuantity
}

public sealed record ProcessReturnResult(ProcessReturnOutcome Outcome, int? SaleReturnId, decimal? TotalRefund, int? FailedSaleLineItemId);

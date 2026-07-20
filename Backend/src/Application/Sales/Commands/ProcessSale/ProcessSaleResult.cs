namespace Application.Sales.Commands.ProcessSale;

public sealed record ProcessSaleResult(
    ProcessSaleOutcome Outcome,
    int? SaleTransactionId,
    decimal? TotalAmount,
    string? Currency,
    int? FailedProductId,
    decimal? GiftCardAmountApplied = null,
    decimal? AmountDue = null,
    decimal? StoreCreditAmountApplied = null);

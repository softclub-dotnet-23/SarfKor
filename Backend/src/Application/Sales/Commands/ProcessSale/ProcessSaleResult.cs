namespace Application.Sales.Commands.ProcessSale;

// SaleLineItemId is the input ProcessReturnCommand needs to return a specific line — it's
// otherwise unrecoverable, since there is no "get sale details" query.
public sealed record ProcessSaleLineResultDto(int SaleLineItemId, int ProductId, int Quantity, decimal UnitPrice);

public sealed record ProcessSaleResult(
    ProcessSaleOutcome Outcome,
    int? SaleTransactionId,
    decimal? TotalAmount,
    string? Currency,
    int? FailedProductId,
    decimal? GiftCardAmountApplied = null,
    decimal? AmountDue = null,
    decimal? StoreCreditAmountApplied = null,
    IReadOnlyList<ProcessSaleLineResultDto>? Lines = null);

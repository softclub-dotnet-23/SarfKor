namespace Application.Sales.Queries.GetReturnsForSale;

public sealed record ReturnLineDto(int SaleLineItemId, int Quantity, decimal RefundAmount);

public sealed record SaleReturnDto(int SaleReturnId, string Reason, DateTimeOffset CreatedAt, IReadOnlyList<ReturnLineDto> Lines);

public enum GetReturnsForSaleOutcome
{
    Found,
    SaleNotFound,
    Forbidden
}

public sealed record GetReturnsForSaleResult(GetReturnsForSaleOutcome Outcome, IReadOnlyList<SaleReturnDto>? Returns);

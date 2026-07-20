namespace Application.Sales.Queries.GetCommissionsForSale;

public sealed record CommissionDto(int CommissionId, string CashierUserId, decimal Amount, string Currency, DateTimeOffset CreatedAt);

public enum GetCommissionsForSaleOutcome
{
    Found,
    SaleNotFound,
    Forbidden
}

public sealed record GetCommissionsForSaleResult(GetCommissionsForSaleOutcome Outcome, IReadOnlyList<CommissionDto>? Commissions);

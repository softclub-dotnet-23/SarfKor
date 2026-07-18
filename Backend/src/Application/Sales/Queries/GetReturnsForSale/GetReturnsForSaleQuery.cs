namespace Application.Sales.Queries.GetReturnsForSale;

public sealed record GetReturnsForSaleQuery(int SaleTransactionId, string RequestedByUserId);

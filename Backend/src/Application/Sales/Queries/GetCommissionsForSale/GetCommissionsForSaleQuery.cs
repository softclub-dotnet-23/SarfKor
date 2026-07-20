namespace Application.Sales.Queries.GetCommissionsForSale;

public sealed record GetCommissionsForSaleQuery(int SaleTransactionId, string RequestedByUserId);

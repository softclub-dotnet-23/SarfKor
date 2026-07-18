namespace Application.Sales.Commands.VoidSale;

public sealed record VoidSaleCommand(int SaleTransactionId, string VoidedByUserId, string Reason);

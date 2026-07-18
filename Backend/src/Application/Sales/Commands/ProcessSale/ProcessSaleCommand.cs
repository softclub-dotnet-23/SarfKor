namespace Application.Sales.Commands.ProcessSale;

public sealed record ProcessSaleLine(int ProductId, int Quantity);

public sealed record ProcessSaleCommand(
    int StoreId,
    string CashierUserId,
    string IdempotencyKey,
    string Currency,
    IReadOnlyList<ProcessSaleLine> Lines);

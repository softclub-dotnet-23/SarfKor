namespace Application.Sales.Commands.ProcessSale;

public enum ProcessSaleOutcome
{
    Completed,
    StoreNotFound,
    Forbidden,
    ProductNotFound,
    PriceNotFound,
    InsufficientStock
}

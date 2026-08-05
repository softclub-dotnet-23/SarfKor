namespace Application.Sales.Commands.ProcessSale;

public enum ProcessSaleOutcome
{
    Completed,
    StoreNotFound,
    Forbidden,
    SubscriptionInactive,
    ProductNotFound,
    PriceNotFound,
    InsufficientStock,
    GiftCardNotFound,
    GiftCardNotUsable,
    GiftCardInsufficientBalance,
    StoreCreditInsufficientBalance,
    CustomerNotFound,
    BundleNotFound
}

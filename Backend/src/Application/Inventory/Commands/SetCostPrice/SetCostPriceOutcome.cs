namespace Application.Inventory.Commands.SetCostPrice;

public enum SetCostPriceOutcome
{
    Set,
    StoreNotFound,
    ProductNotFound,
    Forbidden,
    SubscriptionInactive
}

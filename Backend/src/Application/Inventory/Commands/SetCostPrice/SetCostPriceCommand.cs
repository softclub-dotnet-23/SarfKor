namespace Application.Inventory.Commands.SetCostPrice;

public sealed record SetCostPriceCommand(int StoreId, int ProductId, decimal Amount, string Currency, string PerformedByUserId);

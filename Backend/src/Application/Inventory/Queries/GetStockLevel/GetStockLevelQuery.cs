namespace Application.Inventory.Queries.GetStockLevel;

public sealed record GetStockLevelQuery(int StoreId, string RequestedByUserId);

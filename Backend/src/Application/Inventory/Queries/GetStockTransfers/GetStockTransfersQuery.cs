namespace Application.Inventory.Queries.GetStockTransfers;

public sealed record GetStockTransfersQuery(int StoreId, string RequestedByUserId);

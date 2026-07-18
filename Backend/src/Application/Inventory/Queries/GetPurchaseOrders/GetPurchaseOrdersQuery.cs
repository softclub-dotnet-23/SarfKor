namespace Application.Inventory.Queries.GetPurchaseOrders;

public sealed record GetPurchaseOrdersQuery(int StoreId, string RequestedByUserId);

namespace Application.Inventory.Queries.GetReorderAlerts;

public sealed record GetReorderAlertsQuery(int StoreId, string RequestedByUserId);

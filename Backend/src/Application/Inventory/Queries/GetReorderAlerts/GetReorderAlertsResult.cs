namespace Application.Inventory.Queries.GetReorderAlerts;

public sealed record ReorderAlertDto(int ProductId, int CurrentQuantity, int ThresholdQuantity, int ReorderQuantity, int? PreferredSupplierId);

public enum GetReorderAlertsOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetReorderAlertsResult(GetReorderAlertsOutcome Outcome, IReadOnlyList<ReorderAlertDto>? Alerts);

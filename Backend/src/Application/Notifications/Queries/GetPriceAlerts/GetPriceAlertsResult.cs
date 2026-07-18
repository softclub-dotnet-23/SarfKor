namespace Application.Notifications.Queries.GetPriceAlerts;

public sealed record PriceAlertDto(int PriceAlertId, int ProductId, decimal TargetPrice, string Currency, bool IsActive);

public sealed record GetPriceAlertsResult(IReadOnlyList<PriceAlertDto> Alerts);

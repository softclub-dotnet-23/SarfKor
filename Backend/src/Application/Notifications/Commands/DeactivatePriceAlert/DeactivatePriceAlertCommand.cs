namespace Application.Notifications.Commands.DeactivatePriceAlert;

public sealed record DeactivatePriceAlertCommand(int PriceAlertId, string UserId);

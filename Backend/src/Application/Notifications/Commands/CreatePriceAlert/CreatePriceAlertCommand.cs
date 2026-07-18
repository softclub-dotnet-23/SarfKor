namespace Application.Notifications.Commands.CreatePriceAlert;

public sealed record CreatePriceAlertCommand(string UserId, int ProductId, decimal TargetPrice, string Currency);

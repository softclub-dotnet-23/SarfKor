namespace Application.Notifications.Commands.DeactivatePriceAlert;

public enum DeactivatePriceAlertOutcome
{
    Deactivated,
    NotFound,
    Forbidden
}

public sealed record DeactivatePriceAlertResult(DeactivatePriceAlertOutcome Outcome);

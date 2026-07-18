namespace Application.Notifications.Commands.MarkNotificationAsRead;

public enum MarkNotificationAsReadOutcome
{
    MarkedRead,
    NotFound,
    Forbidden
}

public sealed record MarkNotificationAsReadResult(MarkNotificationAsReadOutcome Outcome);

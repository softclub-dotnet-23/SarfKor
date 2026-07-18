using Domain.Common;

namespace Domain.Notifications;

public class Notification : Entity
{
    public required string UserId { get; set; }
    public NotificationType Type { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

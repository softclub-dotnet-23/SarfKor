using Domain.Notifications;

namespace Application.Notifications.Queries.GetNotifications;

public sealed record NotificationDto(int NotificationId, NotificationType Type, string Message, bool IsRead, DateTimeOffset CreatedAt);

public sealed record GetNotificationsResult(IReadOnlyList<NotificationDto> Notifications);

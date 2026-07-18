using Application.Abstractions;
using Application.Common;

namespace Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(INotificationRepository notificationRepository) : IQueryHandler<GetNotificationsQuery, GetNotificationsResult>
{
    public async Task<GetNotificationsResult> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return new GetNotificationsResult(notifications
            .Select(n => new NotificationDto(n.Id, n.Type, n.Message, n.IsRead, n.CreatedAt))
            .ToList());
    }
}

using Domain.Notifications;

namespace Application.Abstractions;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int notificationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(Notification notification);
}

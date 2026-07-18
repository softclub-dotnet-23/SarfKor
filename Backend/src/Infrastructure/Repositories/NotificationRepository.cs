using Application.Abstractions;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class NotificationRepository(AppDbContext dbContext) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(int notificationId, CancellationToken cancellationToken) =>
        dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);

    public void Add(Notification notification) => dbContext.Notifications.Add(notification);
}

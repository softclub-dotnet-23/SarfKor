using Application.Abstractions;
using Application.Notifications.Queries.GetNotifications;
using Domain.Notifications;
using Moq;

namespace Application.Tests;

public class GetNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsersNotifications()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync([
            new Notification { UserId = "user-1", Type = NotificationType.PriceDrop, Message = "Price dropped!", IsRead = false, CreatedAt = DateTimeOffset.UtcNow }
        ]);

        var handler = new GetNotificationsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetNotificationsQuery("user-1"), CancellationToken.None);

        Assert.Single(result.Notifications);
        Assert.Equal(NotificationType.PriceDrop, result.Notifications[0].Type);
        Assert.False(result.Notifications[0].IsRead);
    }
}

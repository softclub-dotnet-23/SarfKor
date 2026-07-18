using Application.Abstractions;
using Application.Notifications.Commands.MarkNotificationAsRead;
using Domain.Notifications;
using Moq;

namespace Application.Tests;

public class MarkNotificationAsReadCommandHandlerTests
{
    private const string OwnerId = "user-1";
    private const int NotificationId = 1;

    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private MarkNotificationAsReadCommandHandler CreateHandler() => new(_notificationRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsNotFound()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(NotificationId, It.IsAny<CancellationToken>())).ReturnsAsync((Notification?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new MarkNotificationAsReadCommand(NotificationId, OwnerId), CancellationToken.None);

        Assert.Equal(MarkNotificationAsReadOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _notificationRepository
            .Setup(r => r.GetByIdAsync(NotificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notification { UserId = OwnerId, Type = NotificationType.LowStock, Message = "Low stock", IsRead = false, CreatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(new MarkNotificationAsReadCommand(NotificationId, "someone-else"), CancellationToken.None);

        Assert.Equal(MarkNotificationAsReadOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_MarksRead()
    {
        var notification = new Notification { UserId = OwnerId, Type = NotificationType.LowStock, Message = "Low stock", IsRead = false, CreatedAt = DateTimeOffset.UtcNow };
        _notificationRepository.Setup(r => r.GetByIdAsync(NotificationId, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var handler = CreateHandler();
        var result = await handler.Handle(new MarkNotificationAsReadCommand(NotificationId, OwnerId), CancellationToken.None);

        Assert.Equal(MarkNotificationAsReadOutcome.MarkedRead, result.Outcome);
        Assert.True(notification.IsRead);
    }
}

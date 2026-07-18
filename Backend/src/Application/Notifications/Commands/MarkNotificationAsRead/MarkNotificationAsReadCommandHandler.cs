using Application.Abstractions;
using Application.Common;

namespace Application.Notifications.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadResult>
{
    public async Task<MarkNotificationAsReadResult> Handle(MarkNotificationAsReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);
        if (notification is null)
            return new MarkNotificationAsReadResult(MarkNotificationAsReadOutcome.NotFound);

        if (notification.UserId != command.UserId)
            return new MarkNotificationAsReadResult(MarkNotificationAsReadOutcome.Forbidden);

        notification.IsRead = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MarkNotificationAsReadResult(MarkNotificationAsReadOutcome.MarkedRead);
    }
}

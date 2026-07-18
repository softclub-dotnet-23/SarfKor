using FluentValidation;

namespace Application.Notifications.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(x => x.NotificationId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

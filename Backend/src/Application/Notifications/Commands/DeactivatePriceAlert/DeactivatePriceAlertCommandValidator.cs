using FluentValidation;

namespace Application.Notifications.Commands.DeactivatePriceAlert;

public sealed class DeactivatePriceAlertCommandValidator : AbstractValidator<DeactivatePriceAlertCommand>
{
    public DeactivatePriceAlertCommandValidator()
    {
        RuleFor(x => x.PriceAlertId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

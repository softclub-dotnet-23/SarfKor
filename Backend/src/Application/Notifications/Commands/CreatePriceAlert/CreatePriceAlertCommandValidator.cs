using FluentValidation;

namespace Application.Notifications.Commands.CreatePriceAlert;

public sealed class CreatePriceAlertCommandValidator : AbstractValidator<CreatePriceAlertCommand>
{
    public CreatePriceAlertCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.TargetPrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

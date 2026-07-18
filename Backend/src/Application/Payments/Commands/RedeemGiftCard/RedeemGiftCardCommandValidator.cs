using FluentValidation;

namespace Application.Payments.Commands.RedeemGiftCard;

public sealed class RedeemGiftCardCommandValidator : AbstractValidator<RedeemGiftCardCommand>
{
    public RedeemGiftCardCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

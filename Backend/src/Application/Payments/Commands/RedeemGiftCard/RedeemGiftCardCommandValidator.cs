using Application.Common;
using FluentValidation;

namespace Application.Payments.Commands.RedeemGiftCard;

public sealed class RedeemGiftCardCommandValidator : AbstractValidator<RedeemGiftCardCommand>
{
    public RedeemGiftCardCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}

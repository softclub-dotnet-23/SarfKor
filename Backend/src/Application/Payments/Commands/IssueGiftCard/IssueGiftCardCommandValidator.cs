using Application.Common;
using FluentValidation;

namespace Application.Payments.Commands.IssueGiftCard;

public sealed class IssueGiftCardCommandValidator : AbstractValidator<IssueGiftCardCommand>
{
    public IssueGiftCardCommandValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Currency).NotEmpty().Must(SupportedCurrencies.IsSupported).WithMessage("Unsupported currency.");
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow).When(x => x.ExpiresAt.HasValue);
    }
}

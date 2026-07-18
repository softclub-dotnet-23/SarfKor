using FluentValidation;

namespace Application.Payments.Commands.IssueGiftCard;

public sealed class IssueGiftCardCommandValidator : AbstractValidator<IssueGiftCardCommand>
{
    public IssueGiftCardCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow).When(x => x.ExpiresAt.HasValue);
    }
}

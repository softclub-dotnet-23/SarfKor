using FluentValidation;

namespace Application.Subscriptions.Commands.ReverseSubscriptionPayment;

public sealed class ReverseSubscriptionPaymentCommandValidator : AbstractValidator<ReverseSubscriptionPaymentCommand>
{
    public ReverseSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.SubscriptionPaymentId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}

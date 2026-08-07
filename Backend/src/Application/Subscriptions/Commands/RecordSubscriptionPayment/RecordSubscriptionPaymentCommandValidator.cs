using FluentValidation;

namespace Application.Subscriptions.Commands.RecordSubscriptionPayment;

public sealed class RecordSubscriptionPaymentCommandValidator : AbstractValidator<RecordSubscriptionPaymentCommand>
{
    public RecordSubscriptionPaymentCommandValidator()
    {
        RuleFor(x => x.StoreSubscriptionId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}

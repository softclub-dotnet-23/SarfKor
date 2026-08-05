using FluentValidation;

namespace Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public sealed class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.SubscriptionPlanId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MonthlyPriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MonthlyPriceCurrency).NotEmpty().Length(3);
        RuleFor(x => x.MaxStores).GreaterThan(0).When(x => x.MaxStores is not null);
        RuleFor(x => x.MaxEmployees).GreaterThan(0).When(x => x.MaxEmployees is not null);
    }
}

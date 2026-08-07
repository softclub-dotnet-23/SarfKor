using FluentValidation;

namespace Application.Subscriptions.Commands.CreateSubscriptionPlan;

public sealed class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
{
    public CreateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[a-z0-9-]+$")
            .WithMessage("Code must be lowercase letters, digits, and hyphens only.");
        RuleFor(x => x.MonthlyPriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MonthlyPriceCurrency).NotEmpty().Length(3);
        RuleFor(x => x.MaxStores).GreaterThan(0).When(x => x.MaxStores is not null);
        RuleFor(x => x.MaxEmployees).GreaterThan(0).When(x => x.MaxEmployees is not null);
    }
}

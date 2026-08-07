using FluentValidation;

namespace Application.Subscriptions.Commands.ChangeStoreSubscriptionPlan;

public sealed class ChangeStoreSubscriptionPlanCommandValidator : AbstractValidator<ChangeStoreSubscriptionPlanCommand>
{
    public ChangeStoreSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.StoreSubscriptionId).GreaterThan(0);
        RuleFor(x => x.NewSubscriptionPlanId).GreaterThan(0);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}

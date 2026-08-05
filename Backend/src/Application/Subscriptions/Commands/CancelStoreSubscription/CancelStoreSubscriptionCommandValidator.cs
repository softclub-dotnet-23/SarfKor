using FluentValidation;

namespace Application.Subscriptions.Commands.CancelStoreSubscription;

public sealed class CancelStoreSubscriptionCommandValidator : AbstractValidator<CancelStoreSubscriptionCommand>
{
    public CancelStoreSubscriptionCommandValidator()
    {
        RuleFor(x => x.StoreSubscriptionId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PerformedByUserId).NotEmpty();
    }
}

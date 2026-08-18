using FluentValidation;

namespace Application.Subscriptions.Queries.GetMyStoreSubscriptionStatus;

public sealed class GetMyStoreSubscriptionStatusQueryValidator : AbstractValidator<GetMyStoreSubscriptionStatusQuery>
{
    public GetMyStoreSubscriptionStatusQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

using FluentValidation;

namespace Application.Subscriptions.Queries.GetStoreSubscriptions;

public sealed class GetStoreSubscriptionsQueryValidator : AbstractValidator<GetStoreSubscriptionsQuery>
{
    public GetStoreSubscriptionsQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

using FluentValidation;

namespace Application.Subscriptions.Queries.GetSubscriptionPayments;

public sealed class GetSubscriptionPaymentsQueryValidator : AbstractValidator<GetSubscriptionPaymentsQuery>
{
    public GetSubscriptionPaymentsQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

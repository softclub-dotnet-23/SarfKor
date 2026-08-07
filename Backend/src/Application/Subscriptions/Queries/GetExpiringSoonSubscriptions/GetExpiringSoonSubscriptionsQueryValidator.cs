using FluentValidation;

namespace Application.Subscriptions.Queries.GetExpiringSoonSubscriptions;

public sealed class GetExpiringSoonSubscriptionsQueryValidator : AbstractValidator<GetExpiringSoonSubscriptionsQuery>
{
    public GetExpiringSoonSubscriptionsQueryValidator()
    {
        RuleFor(x => x.WithinDays).InclusiveBetween(1, 90);
    }
}

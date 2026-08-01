using FluentValidation;

namespace Application.Loyalty.Queries.GetLoyaltyAccount;

public sealed class GetLoyaltyAccountQueryValidator : AbstractValidator<GetLoyaltyAccountQuery>
{
    public GetLoyaltyAccountQueryValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.LoyaltyProgramId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

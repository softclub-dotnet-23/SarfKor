using FluentValidation;

namespace Application.Loyalty.Queries.GetLoyaltyProgram;

public sealed class GetLoyaltyProgramQueryValidator : AbstractValidator<GetLoyaltyProgramQuery>
{
    public GetLoyaltyProgramQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

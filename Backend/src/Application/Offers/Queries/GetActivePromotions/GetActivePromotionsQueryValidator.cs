using FluentValidation;

namespace Application.Offers.Queries.GetActivePromotions;

public sealed class GetActivePromotionsQueryValidator : AbstractValidator<GetActivePromotionsQuery>
{
    public GetActivePromotionsQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

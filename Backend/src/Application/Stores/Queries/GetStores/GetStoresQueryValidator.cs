using FluentValidation;

namespace Application.Stores.Queries.GetStores;

public sealed class GetStoresQueryValidator : AbstractValidator<GetStoresQuery>
{
    public GetStoresQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

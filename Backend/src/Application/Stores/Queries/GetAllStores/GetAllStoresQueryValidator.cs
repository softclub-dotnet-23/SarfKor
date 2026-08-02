using FluentValidation;

namespace Application.Stores.Queries.GetAllStores;

public sealed class GetAllStoresQueryValidator : AbstractValidator<GetAllStoresQuery>
{
    public GetAllStoresQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

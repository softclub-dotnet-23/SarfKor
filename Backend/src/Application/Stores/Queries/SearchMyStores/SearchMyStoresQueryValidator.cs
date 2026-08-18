using FluentValidation;

namespace Application.Stores.Queries.SearchMyStores;

public sealed class SearchMyStoresQueryValidator : AbstractValidator<SearchMyStoresQuery>
{
    public SearchMyStoresQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 50);
    }
}

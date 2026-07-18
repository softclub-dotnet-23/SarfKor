using FluentValidation;

namespace Application.Products.Queries.CompareStoresForShoppingList;

public sealed class CompareStoresForShoppingListQueryValidator : AbstractValidator<CompareStoresForShoppingListQuery>
{
    public CompareStoresForShoppingListQueryValidator()
    {
        RuleFor(x => x.ProductIds).NotEmpty();
        RuleForEach(x => x.ProductIds).GreaterThan(0);
        RuleFor(x => x.UserLatitude).InclusiveBetween(-90, 90).When(x => x.UserLatitude.HasValue);
        RuleFor(x => x.UserLongitude).InclusiveBetween(-180, 180).When(x => x.UserLongitude.HasValue);
    }
}

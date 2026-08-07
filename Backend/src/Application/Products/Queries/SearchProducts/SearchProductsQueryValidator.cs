using FluentValidation;

namespace Application.Products.Queries.SearchProducts;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        // Small pages — this feeds an infinite-scroll combobox, not a report; the frontend asks
        // again as the user scrolls rather than pulling the whole catalog up front.
        RuleFor(x => x.Take).InclusiveBetween(1, 50);
    }
}

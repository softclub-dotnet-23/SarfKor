using FluentValidation;

namespace Application.Products.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsQueryValidator : AbstractValidator<GetTopSellingProductsQuery>
{
    public GetTopSellingProductsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
    }
}

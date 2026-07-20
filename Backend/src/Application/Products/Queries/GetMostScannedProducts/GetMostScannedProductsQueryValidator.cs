using FluentValidation;

namespace Application.Products.Queries.GetMostScannedProducts;

public sealed class GetMostScannedProductsQueryValidator : AbstractValidator<GetMostScannedProductsQuery>
{
    public GetMostScannedProductsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}

using FluentValidation;

namespace Application.Catalog.Queries.GetProductBundles;

public sealed class GetProductBundlesQueryValidator : AbstractValidator<GetProductBundlesQuery>
{
    public GetProductBundlesQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

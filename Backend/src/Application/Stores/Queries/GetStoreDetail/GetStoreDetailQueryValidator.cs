using FluentValidation;

namespace Application.Stores.Queries.GetStoreDetail;

public sealed class GetStoreDetailQueryValidator : AbstractValidator<GetStoreDetailQuery>
{
    public GetStoreDetailQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

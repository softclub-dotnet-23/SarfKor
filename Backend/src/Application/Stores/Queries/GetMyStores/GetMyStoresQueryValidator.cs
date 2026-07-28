using FluentValidation;

namespace Application.Stores.Queries.GetMyStores;

public sealed class GetMyStoresQueryValidator : AbstractValidator<GetMyStoresQuery>
{
    public GetMyStoresQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

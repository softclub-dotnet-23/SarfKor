using FluentValidation;

namespace Application.Stores.Queries.GetStoreLocations;

public sealed class GetStoreLocationsQueryValidator : AbstractValidator<GetStoreLocationsQuery>
{
    public GetStoreLocationsQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

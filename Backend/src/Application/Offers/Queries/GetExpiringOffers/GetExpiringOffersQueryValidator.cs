using FluentValidation;

namespace Application.Offers.Queries.GetExpiringOffers;

public sealed class GetExpiringOffersQueryValidator : AbstractValidator<GetExpiringOffersQuery>
{
    public GetExpiringOffersQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0).When(x => x.StoreId.HasValue);
        RuleFor(x => x.UserLatitude).InclusiveBetween(-90, 90).When(x => x.UserLatitude.HasValue);
        RuleFor(x => x.UserLongitude).InclusiveBetween(-180, 180).When(x => x.UserLongitude.HasValue);
    }
}

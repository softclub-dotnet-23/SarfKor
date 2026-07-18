using FluentValidation;

namespace Application.Stores.Queries.GetStoreDashboard;

public sealed class GetStoreDashboardQueryValidator : AbstractValidator<GetStoreDashboardQuery>
{
    public GetStoreDashboardQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

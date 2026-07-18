using FluentValidation;

namespace Application.Inventory.Queries.GetReorderAlerts;

public sealed class GetReorderAlertsQueryValidator : AbstractValidator<GetReorderAlertsQuery>
{
    public GetReorderAlertsQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

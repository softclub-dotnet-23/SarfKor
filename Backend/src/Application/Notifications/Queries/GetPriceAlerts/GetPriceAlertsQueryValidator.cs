using FluentValidation;

namespace Application.Notifications.Queries.GetPriceAlerts;

public sealed class GetPriceAlertsQueryValidator : AbstractValidator<GetPriceAlertsQuery>
{
    public GetPriceAlertsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

using Application.Abstractions;
using Application.Common;

namespace Application.Notifications.Queries.GetPriceAlerts;

public sealed class GetPriceAlertsQueryHandler(IPriceAlertRepository priceAlertRepository) : IQueryHandler<GetPriceAlertsQuery, GetPriceAlertsResult>
{
    public async Task<GetPriceAlertsResult> Handle(GetPriceAlertsQuery query, CancellationToken cancellationToken)
    {
        var alerts = await priceAlertRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return new GetPriceAlertsResult(alerts
            .Select(a => new PriceAlertDto(a.Id, a.ProductId, a.TargetPrice.Amount, a.TargetPrice.Currency, a.IsActive))
            .ToList());
    }
}

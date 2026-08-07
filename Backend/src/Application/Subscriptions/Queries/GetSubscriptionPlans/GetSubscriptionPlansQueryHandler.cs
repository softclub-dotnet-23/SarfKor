using System.Text.Json;
using Application.Abstractions;
using Application.Common;

namespace Application.Subscriptions.Queries.GetSubscriptionPlans;

public sealed class GetSubscriptionPlansQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository)
    : IQueryHandler<GetSubscriptionPlansQuery, GetSubscriptionPlansResult>
{
    public async Task<GetSubscriptionPlansResult> Handle(GetSubscriptionPlansQuery query, CancellationToken cancellationToken)
    {
        var plans = await subscriptionPlanRepository.GetAllAsync(query.IncludeInactive, cancellationToken);
        var dtos = plans.Select(p => new SubscriptionPlanDto(
            p.Id,
            p.Name,
            p.Code,
            p.MonthlyPrice.Amount,
            p.MonthlyPrice.Currency,
            p.MaxStores,
            p.MaxEmployees,
            p.FeaturesJson is not null ? JsonSerializer.Deserialize<List<string>>(p.FeaturesJson) ?? [] : [],
            p.IsActive)).ToList();
        return new GetSubscriptionPlansResult(dtos);
    }
}

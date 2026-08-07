namespace Application.Subscriptions.Queries.GetSubscriptionPlans;

public sealed record SubscriptionPlanDto(
    int SubscriptionPlanId,
    string Name,
    string Code,
    decimal MonthlyPriceAmount,
    string MonthlyPriceCurrency,
    int? MaxStores,
    int? MaxEmployees,
    IReadOnlyList<string> Features,
    bool IsActive);

public sealed record GetSubscriptionPlansResult(IReadOnlyList<SubscriptionPlanDto> Plans);

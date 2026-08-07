namespace Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanCommand(
    int SubscriptionPlanId,
    string Name,
    decimal MonthlyPriceAmount,
    string MonthlyPriceCurrency,
    int? MaxStores,
    int? MaxEmployees,
    IReadOnlyList<string>? Features,
    bool IsActive,
    string PerformedByUserId,
    string? PerformedByIpAddress = null);

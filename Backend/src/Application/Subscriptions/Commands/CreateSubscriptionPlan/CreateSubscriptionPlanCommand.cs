namespace Application.Subscriptions.Commands.CreateSubscriptionPlan;

public sealed record CreateSubscriptionPlanCommand(
    string Name,
    string Code,
    decimal MonthlyPriceAmount,
    string MonthlyPriceCurrency,
    int? MaxStores,
    int? MaxEmployees,
    IReadOnlyList<string>? Features,
    string PerformedByUserId,
    string? PerformedByIpAddress = null);

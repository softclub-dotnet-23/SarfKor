namespace Application.Subscriptions.Commands.ChangeStoreSubscriptionPlan;

public sealed record ChangeStoreSubscriptionPlanCommand(int StoreSubscriptionId, int NewSubscriptionPlanId, string PerformedByUserId, string? PerformedByIpAddress = null);

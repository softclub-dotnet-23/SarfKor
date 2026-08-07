namespace Application.Subscriptions.Commands.CancelStoreSubscription;

public sealed record CancelStoreSubscriptionCommand(int StoreSubscriptionId, string Reason, string PerformedByUserId, string? PerformedByIpAddress = null);

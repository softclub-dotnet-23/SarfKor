namespace Application.Subscriptions.Commands.CancelStoreSubscription;

public enum CancelStoreSubscriptionOutcome
{
    Cancelled,
    NotFound,
    AlreadyCancelled
}

public sealed record CancelStoreSubscriptionResult(CancelStoreSubscriptionOutcome Outcome);

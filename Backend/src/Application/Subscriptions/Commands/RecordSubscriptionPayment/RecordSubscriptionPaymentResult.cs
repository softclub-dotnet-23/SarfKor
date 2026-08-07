namespace Application.Subscriptions.Commands.RecordSubscriptionPayment;

public enum RecordSubscriptionPaymentOutcome
{
    Recorded,
    SubscriptionNotFound
}

public sealed record RecordSubscriptionPaymentResult(RecordSubscriptionPaymentOutcome Outcome, int? SubscriptionPaymentId, DateTimeOffset? NewPeriodEndsAt);

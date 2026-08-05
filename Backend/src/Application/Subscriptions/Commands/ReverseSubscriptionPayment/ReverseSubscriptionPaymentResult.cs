namespace Application.Subscriptions.Commands.ReverseSubscriptionPayment;

public enum ReverseSubscriptionPaymentOutcome
{
    Reversed,
    NotFound,
    AlreadyReversed
}

public sealed record ReverseSubscriptionPaymentResult(ReverseSubscriptionPaymentOutcome Outcome, int? ReversalPaymentId);

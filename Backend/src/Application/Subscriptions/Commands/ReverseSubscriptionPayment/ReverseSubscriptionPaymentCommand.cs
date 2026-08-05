namespace Application.Subscriptions.Commands.ReverseSubscriptionPayment;

public sealed record ReverseSubscriptionPaymentCommand(int SubscriptionPaymentId, string Reason, string PerformedByUserId, string? PerformedByIpAddress = null);

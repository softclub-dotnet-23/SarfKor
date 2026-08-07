using Domain.Subscriptions;

namespace Application.Subscriptions.Commands.RecordSubscriptionPayment;

public sealed record RecordSubscriptionPaymentCommand(
    int StoreSubscriptionId,
    decimal Amount,
    string Currency,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    SubscriptionPaymentMethod Method,
    string? Comment,
    string PerformedByUserId,
    string? PerformedByIpAddress = null);

using Domain.Subscriptions;

namespace Application.Subscriptions.Queries.GetSubscriptionPayments;

public sealed record SubscriptionPaymentDto(
    int SubscriptionPaymentId,
    int StoreSubscriptionId,
    int StoreId,
    string StoreName,
    decimal Amount,
    string Currency,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    SubscriptionPaymentMethod Method,
    string? Comment,
    string? RecordedByUserId,
    string? RecordedByEmail,
    DateTimeOffset RecordedAt,
    bool IsReversal,
    int? ReversedPaymentId);

public sealed record GetSubscriptionPaymentsResult(IReadOnlyList<SubscriptionPaymentDto> Payments, int TotalCount);

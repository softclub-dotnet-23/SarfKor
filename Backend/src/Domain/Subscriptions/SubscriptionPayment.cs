using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Subscriptions;

// Immutable once written — a correction is a new row with IsReversal=true and ReversedPaymentId
// pointing back at the original, never an UPDATE/DELETE of the original (ADMIN_PROMPT.md §2.1:
// "исправление делается сторнирующей записью, а не редактированием").
public class SubscriptionPayment : Entity
{
    public int StoreSubscriptionId { get; set; }
    public required Money Amount { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public SubscriptionPaymentMethod Method { get; set; }
    public string? Comment { get; set; }

    public SubscriptionPaymentSource Source { get; set; } = SubscriptionPaymentSource.ManualAdmin;

    // Null only when Source is ever Automated in the future — every ManualAdmin payment has one.
    public string? RecordedByUserId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public bool IsReversal { get; set; }
    public int? ReversedPaymentId { get; set; }
}

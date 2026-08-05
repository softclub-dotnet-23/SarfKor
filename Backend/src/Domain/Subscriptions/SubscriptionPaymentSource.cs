namespace Domain.Subscriptions;

// ManualAdmin is the only source implemented today (see ADMIN_PROMPT.md §2.1 — no acquiring
// integration yet), but the column exists from day one so a future automated payment source never
// needs a schema change, just a new enum value and a handler that doesn't require RecordedByUserId.
public enum SubscriptionPaymentSource
{
    ManualAdmin,
    Automated
}

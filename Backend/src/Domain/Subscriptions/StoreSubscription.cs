using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Subscriptions;

// One row per store, current subscription state — history of *changes* (plan swaps, status
// transitions, suspensions) lives in AuditLog, same split as Store.Status/StatusReason above it.
public class StoreSubscription : Entity
{
    public int StoreId { get; set; }
    public int SubscriptionPlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CurrentPeriodEndsAt { get; set; }

    // Fixed at issue time so a later plan price change never rewrites what this store is actually
    // being billed for historically — SubscriptionPayment amounts are compared against this, not
    // against SubscriptionPlan.MonthlyPrice, which can move on.
    public required Money PriceAtIssue { get; set; }

    public string? Note { get; set; }
}

using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Subscriptions;

// Reference table of tariffs an Admin maintains. IsActive=false ("hidden") keeps a retired plan
// assignable to nothing new while StoreSubscriptions already issued against it keep working
// unchanged — StoreSubscription.PriceAtIssue is what actually bills, not a live read of this row.
public class SubscriptionPlan : Entity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required Money MonthlyPrice { get; set; }
    public int? MaxStores { get; set; }
    public int? MaxEmployees { get; set; }

    // JSON string array, not a normalized table — a plan's feature list is display-only copy
    // ("what this tier includes"), never queried/filtered on, so a second table would be pure
    // ceremony for the same reason Notification/AuditLog use a free-text Details column elsewhere.
    public string? FeaturesJson { get; set; }

    public bool IsActive { get; set; } = true;
}

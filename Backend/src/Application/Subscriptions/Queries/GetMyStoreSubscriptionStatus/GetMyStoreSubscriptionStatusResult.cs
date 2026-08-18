using Domain.Subscriptions;

namespace Application.Subscriptions.Queries.GetMyStoreSubscriptionStatus;

public enum GetMyStoreSubscriptionStatusOutcome
{
    Found,
    StoreNotFound,
    Forbidden,
}

/// <summary>
/// Deliberately narrow — status + the one date a store owner/cashier actually needs to see when
/// a write action 402s ("когда закончилась/закончится подписка"). No plan name, no price, no
/// payment history: that commercial detail belongs to the platform-Admin subscriptions console,
/// not this every-store-employee-readable endpoint (same trust boundary as GetStoreDiagnostics'
/// own "no commercial data" rule).
///
/// <see cref="IsOperational"/> is computed via the exact same <c>IStoreAccessAuthorizer
/// .IsOperationalAsync</c> every write handler gates on -- deliberately NOT re-derived from
/// <see cref="Status"/> on the frontend. A `null` <see cref="Status"/> (no StoreSubscription row
/// at all) reads as "not current" for display purposes but is actually operational per that
/// authorizer (a store approved before the subscription system existed), so a client-side
/// `status === 'Active'` re-derivation would have wrongly shown "subscription inactive, pay up"
/// banners/disabled buttons to stores that were never blocked in the first place. This field is
/// what lets the frontend gate proactively (disable a button, skip opening a modal) with zero risk
/// of drifting from what the backend will actually accept.
/// </summary>
public sealed record GetMyStoreSubscriptionStatusResult(
    GetMyStoreSubscriptionStatusOutcome Outcome,
    SubscriptionStatus? Status,
    DateTimeOffset? CurrentPeriodEndsAt,
    bool? IsOwner,
    bool IsOperational);

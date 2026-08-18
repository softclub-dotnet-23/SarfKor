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
/// </summary>
public sealed record GetMyStoreSubscriptionStatusResult(
    GetMyStoreSubscriptionStatusOutcome Outcome,
    SubscriptionStatus? Status,
    DateTimeOffset? CurrentPeriodEndsAt,
    bool? IsOwner);

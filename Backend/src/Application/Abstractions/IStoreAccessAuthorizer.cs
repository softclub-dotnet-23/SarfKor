namespace Application.Abstractions;

/// <summary>
/// Centralizes the "is this user the store's owner, or one of its employees" check that most
/// store-scoped handlers need beyond the coarse [Authorize(Roles = "StorePartner")] gate.
/// </summary>
public interface IStoreAccessAuthorizer
{
    Task<bool> IsOwnerOrEmployeeAsync(int storeId, string userId, CancellationToken cancellationToken);

    Task<bool> IsOwnerAsync(int storeId, string userId, CancellationToken cancellationToken);

    /// <summary>False if the store is closed for either reason ADMIN_PROMPT.md §2.2 calls out —
    /// administrative (Store.Status is Suspended/Blocked/Archived/PendingApproval/Rejected) or
    /// financial (its StoreSubscription.Status is Suspended) — either one alone is enough to close
    /// the cabinet/register. Public B2C price data is intentionally NOT gated by this — see
    /// IStoreRepository.GetApprovedByIdsAsync, a separate, narrower check.</summary>
    Task<bool> IsOperationalAsync(int storeId, CancellationToken cancellationToken);
}

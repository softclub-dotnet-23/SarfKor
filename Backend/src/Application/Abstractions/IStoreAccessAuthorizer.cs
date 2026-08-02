namespace Application.Abstractions;

/// <summary>
/// Centralizes the "is this user the store's owner, or one of its employees" check that most
/// store-scoped handlers need beyond the coarse [Authorize(Roles = "StorePartner")] gate.
/// </summary>
public interface IStoreAccessAuthorizer
{
    Task<bool> IsOwnerOrEmployeeAsync(int storeId, string userId, CancellationToken cancellationToken);

    Task<bool> IsOwnerAsync(int storeId, string userId, CancellationToken cancellationToken);
}

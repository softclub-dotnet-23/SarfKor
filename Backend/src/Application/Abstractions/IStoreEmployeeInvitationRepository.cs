using Domain.Stores;

namespace Application.Abstractions;

public interface IStoreEmployeeInvitationRepository
{
    Task<StoreEmployeeInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<StoreEmployeeInvitation?> GetByIdAsync(int invitationId, CancellationToken cancellationToken);
    Task<StoreEmployeeInvitation?> GetPendingByStoreAndEmailAsync(int storeId, string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreEmployeeInvitation>> GetByStoreIdAsync(int storeId, StoreEmployeeInvitationStatus? status, CancellationToken cancellationToken);

    /// <summary>Pending rows whose ExpiresAt has already passed — the expiry-sweep background
    /// job's only query.</summary>
    Task<IReadOnlyList<StoreEmployeeInvitation>> GetPendingExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>The platform-wide (StoreId-less) equivalent of GetPendingByStoreAndEmailAsync —
    /// used by CreateUserInvitationCommandHandler to refresh-not-duplicate a re-invite of the same
    /// email to the same role. storeId narrows to a specific store for a StorePartner invite;
    /// null for a platform-wide User/Admin invite (both sides of the comparison — an existing
    /// StorePartner invite for a *different* store must not be treated as a match).</summary>
    Task<StoreEmployeeInvitation?> GetPendingByEmailAndRoleAsync(string email, string invitedRole, int? storeId, CancellationToken cancellationToken);

    /// <summary>Every invitation on the platform (any StoreId, any InvitedRole) — backs
    /// /admin/users' merged users+pending-invitations table. Newest first, same convention as
    /// GetByStoreIdAsync.</summary>
    Task<IReadOnlyList<StoreEmployeeInvitation>> GetAllAsync(CancellationToken cancellationToken);

    void Add(StoreEmployeeInvitation invitation);
}

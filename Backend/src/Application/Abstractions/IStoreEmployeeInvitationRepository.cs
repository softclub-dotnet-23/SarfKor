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

    void Add(StoreEmployeeInvitation invitation);
}

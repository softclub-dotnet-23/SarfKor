using Domain.Stores;

namespace Application.Abstractions;

public interface IStoreEmployeeInvitationRepository
{
    Task<StoreEmployeeInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<StoreEmployeeInvitation?> GetPendingByStoreAndEmailAsync(int storeId, string email, CancellationToken cancellationToken);
    void Add(StoreEmployeeInvitation invitation);
}

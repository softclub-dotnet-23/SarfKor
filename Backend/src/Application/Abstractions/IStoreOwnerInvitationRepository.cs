using Domain.Stores;

namespace Application.Abstractions;

public interface IStoreOwnerInvitationRepository
{
    Task<StoreOwnerInvitation?> GetPendingByEmailAsync(string email, CancellationToken cancellationToken);
    void Add(StoreOwnerInvitation invitation);
}

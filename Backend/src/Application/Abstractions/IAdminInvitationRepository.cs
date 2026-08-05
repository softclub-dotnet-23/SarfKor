using Domain.Identity;

namespace Application.Abstractions;

public interface IAdminInvitationRepository
{
    Task<AdminInvitation?> GetPendingByEmailAsync(string email, CancellationToken cancellationToken);
    void Add(AdminInvitation invitation);
}

using Domain.Identity;

namespace Application.Abstractions;

public interface IUserConsentRepository
{
    Task<IReadOnlyList<UserConsent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<UserConsent?> GetByUserIdAndTypeAsync(string userId, ConsentType type, CancellationToken cancellationToken);
    void Add(UserConsent userConsent);
}

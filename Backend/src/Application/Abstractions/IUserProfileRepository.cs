using Domain.Identity;

namespace Application.Abstractions;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(UserProfile userProfile);
}

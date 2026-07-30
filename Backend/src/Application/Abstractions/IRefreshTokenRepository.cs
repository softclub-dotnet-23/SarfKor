using Domain.Identity;

namespace Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    void Add(RefreshToken refreshToken);
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken);
}

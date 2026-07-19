namespace Application.Abstractions;

public sealed record AuthResult(string UserId, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public interface IAuthService
{
    Task<AuthResult?> RegisterAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthResult?> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken);
}

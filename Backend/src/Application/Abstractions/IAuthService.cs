namespace Application.Abstractions;

public sealed record AuthResult(string UserId, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public interface IAuthService
{
    Task<AuthResult?> RegisterAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthResult?> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken);
    Task RemoveFromRoleAsync(string userId, string role, CancellationToken cancellationToken);
    Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Batched, not one lookup per id — for projecting a page of results (e.g. Admin's
    /// store list) without an N+1 query per row. Ids with no matching/emailless account are simply
    /// absent from the result, not an error.</summary>
    Task<IReadOnlyDictionary<string, string>> GetEmailsByUserIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);

    /// <summary>Null if no account exists for the email — callers must not let that distinguish the response they give back (email enumeration).</summary>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
}

namespace Application.Abstractions;

public sealed record AuthResult(string UserId, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

/// <summary>Auth is set only when registration completed and logged the caller straight in
/// (email pre-verified, e.g. accepting a cashier/store-owner invite that already proved email
/// ownership via its own token/code). For a normal self-registration, Auth stays null,
/// RequiresEmailConfirmation is true, and EmailConfirmationCode carries the plaintext code for the
/// caller to email — AuthService itself never sends mail, callers do (matches every other
/// IEmailSender call site: visible in the handler, wrapped in its own try/catch).
/// EmailAlreadyRegistered narrows down the remaining failure case.</summary>
public sealed record RegisterAccountResult(
    AuthResult? Auth,
    bool EmailAlreadyRegistered,
    bool RequiresEmailConfirmation = false,
    string? EmailConfirmationCode = null);

/// <summary>Auth is null on any failure; EmailNotConfirmed narrows down the one failure case the
/// caller needs to act on differently (prompt to enter/resend the code, not "wrong password").</summary>
public sealed record LoginAccountResult(AuthResult? Auth, bool EmailNotConfirmed);

/// <summary>Auth is set only when the code matched. TooManyAttempts and InvalidOrExpiredCode are
/// mutually exclusive with success and with each other.</summary>
public sealed record ConfirmEmailResult(AuthResult? Auth, bool InvalidOrExpiredCode, bool TooManyAttempts);

public interface IAuthService
{
    /// <summary>emailPreVerified skips the confirmation-code step entirely (the caller already
    /// proved the invitee owns the email via its own token/code, e.g. a store invite) — used only
    /// by AcceptStoreEmployeeInvitationCommandHandler/ConfirmStoreOwnerInvitationCommandHandler,
    /// never by the public self-registration endpoint.</summary>
    Task<RegisterAccountResult> RegisterAsync(string email, string password, bool emailPreVerified, CancellationToken cancellationToken);

    Task<LoginAccountResult> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<ConfirmEmailResult> ConfirmEmailAsync(string email, string code, CancellationToken cancellationToken);
    Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken);
    Task RemoveFromRoleAsync(string userId, string role, CancellationToken cancellationToken);
    Task<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Batched, not one lookup per id — for projecting a page of results (e.g. Admin's
    /// store list) without an N+1 query per row. Ids with no matching/emailless account are simply
    /// absent from the result, not an error.</summary>
    Task<IReadOnlyDictionary<string, string>> GetEmailsByUserIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);

    /// <summary>Null if no account exists for the email — callers must not let that distinguish the response they give back (email enumeration). Reuses the same 6-digit-code mechanics as registration confirmation (same hash/expiry/attempt fields on ApplicationUser), not Identity's opaque token provider.</summary>
    Task<string?> GeneratePasswordResetCodeAsync(string email, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken);
}

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

    /// <summary>The "change password while logged in" path — requires the caller to already know
    /// their current password, unlike ResetPasswordAsync's email-code flow. On success, every
    /// refresh token for the user is revoked the same way ResetPasswordAsync does (a password
    /// change is exactly the moment a hijacked session should be killed), so the caller's own
    /// refresh token stops working too and the access token is left to expire naturally.</summary>
    Task<ChangePasswordServiceResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>Search across the Identity store for ADMIN_PROMPT.md §2.3's user list — Search
    /// matches email (case-insensitive, substring). Lives on IAuthService, not a repository, because
    /// ApplicationUser is an Infrastructure/Identity type Application must not reference directly.</summary>
    Task<(IReadOnlyList<AdminUserSummary> Users, int TotalCount)> SearchUsersAsync(int skip, int take, string? search, CancellationToken cancellationToken);

    Task<AdminUserDetail?> GetUserDetailAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Sets Identity lockout far in the future (same mechanism the failed-login lockout in
    /// LoginAsync already uses) AND revokes every refresh token — ADMIN_PROMPT.md §2.3: "не
    /// полагайся только на проверку при логине". A still-valid access token (≤15 min old) is also
    /// actively rejected — see the JwtBearerEvents.OnTokenValidated hook in Program.cs, which checks
    /// BlockedAt on every authenticated request, not just at login/refresh.</summary>
    Task<bool> BlockUserAsync(string userId, string reason, string performedByAdminUserId, CancellationToken cancellationToken);

    Task<bool> UnblockUserAsync(string userId, CancellationToken cancellationToken);
}

public sealed record AdminUserSummary(string UserId, string? Email, DateTimeOffset CreatedAt, bool IsBlocked, IReadOnlyList<string> Roles);

public sealed record AdminUserDetail(
    string UserId, string? Email, DateTimeOffset CreatedAt, bool IsBlocked,
    string? BlockedReason, DateTimeOffset? BlockedAt, string? BlockedByAdminUserId, IReadOnlyList<string> Roles);

/// <summary>UserNotFound and IncorrectCurrentPassword are mutually exclusive with Succeeded and with
/// each other; Errors carries Identity's password-policy messages when neither of those two applies
/// but Succeeded is still false (e.g. new password fails a strength rule despite matching FluentValidation's
/// own copy of that policy — defense in depth, not expected to normally trigger).</summary>
public sealed record ChangePasswordServiceResult(bool Succeeded, bool UserNotFound, bool IncorrectCurrentPassword, IReadOnlyList<string> Errors);

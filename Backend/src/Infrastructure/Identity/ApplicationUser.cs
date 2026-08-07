using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    // Registration email-confirmation OTP (Identity's own EmailConfirmed flag is what actually
    // gates login — these three only back the 6-digit-code mechanics, mirroring OtpCode's use for
    // StoreOwnerInvitation).
    public string? EmailConfirmationCodeHash { get; set; }
    public DateTimeOffset? EmailConfirmationCodeExpiresAt { get; set; }
    public int EmailConfirmationAttempts { get; set; }

    // Admin-initiated block (ADMIN_PROMPT.md §2.3) — layered on top of Identity's own
    // LockoutEnd/LockoutEnabled (BlockUserCommandHandler sets LockoutEnd far in the future, same
    // mechanism the failed-login lockout in AuthService already uses), these three exist only to
    // answer "is this user blocked, by whom, and why" without re-deriving it from AuditLog. Current
    // state only — full history lives in AuditLog, same split as Store.StatusReason.
    public string? BlockedReason { get; set; }
    public DateTimeOffset? BlockedAt { get; set; }
    public string? BlockedByAdminUserId { get; set; }

    // Not backfillable for accounts created before this column existed (see the migration's data
    // pass, which defaults them to the migration date rather than leaving a misleading year-1 value).
    public DateTimeOffset CreatedAt { get; set; }
}

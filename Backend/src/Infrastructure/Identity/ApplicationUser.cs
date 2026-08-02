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
}

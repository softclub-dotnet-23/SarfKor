using Domain.Stores;

namespace Application.Abstractions;

public interface IEmailSender
{
    /// <summary>Takes the raw 6-digit code, never the hash — no link, the caller types this in.</summary>
    Task SendPasswordResetCodeAsync(string toEmail, string code, CancellationToken cancellationToken);

    /// <summary>One method for every kind of platform invitation (store employee, and /admin/users'
    /// any-role invite) — not one per InvitedRole, matching StoreEmployeeInvitation's own
    /// generalization. Takes the raw invite token, not a URL — same reasoning as the reset email.
    /// storeName/employeeRole are set only when invitedRole is "StorePartner"; language is the
    /// inviting party's own UserProfile.PreferredLanguage ("ru"/"tg"), not the recipient's (unknown
    /// until they've registered).</summary>
    Task SendInvitationEmailAsync(
        string toEmail, string invitedRole, string? storeName, StoreEmployeeRole? employeeRole,
        string inviteToken, int expiryDays, string language, CancellationToken cancellationToken);

    /// <summary>Takes the raw 6-digit code, never the hash — the code exists only in this email.</summary>
    Task SendStoreOwnerInvitationEmailAsync(string toEmail, string storeName, string code, CancellationToken cancellationToken);

    /// <summary>Takes the raw 6-digit code, never the hash — sent right after self-registration.</summary>
    Task SendEmailConfirmationCodeAsync(string toEmail, string code, CancellationToken cancellationToken);

    /// <summary>Takes the raw 6-digit code, never the hash — ADMIN_PROMPT.md §2.7's "second admin
    /// account from an existing admin" invite, same mechanics as SendStoreOwnerInvitationEmailAsync.</summary>
    Task SendAdminInvitationEmailAsync(string toEmail, string code, CancellationToken cancellationToken);
}

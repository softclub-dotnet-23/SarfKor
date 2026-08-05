namespace Application.Abstractions;

public interface IEmailSender
{
    /// <summary>Takes the raw 6-digit code, never the hash — no link, the caller types this in.</summary>
    Task SendPasswordResetCodeAsync(string toEmail, string code, CancellationToken cancellationToken);

    /// <summary>Takes the raw invite token, not a URL — same reasoning as the reset email.</summary>
    Task SendStoreEmployeeInviteEmailAsync(string toEmail, string storeName, string inviteToken, CancellationToken cancellationToken);

    /// <summary>Takes the raw 6-digit code, never the hash — the code exists only in this email.</summary>
    Task SendStoreOwnerInvitationEmailAsync(string toEmail, string storeName, string code, CancellationToken cancellationToken);

    /// <summary>Takes the raw 6-digit code, never the hash — sent right after self-registration.</summary>
    Task SendEmailConfirmationCodeAsync(string toEmail, string code, CancellationToken cancellationToken);

    /// <summary>Takes the raw 6-digit code, never the hash — ADMIN_PROMPT.md §2.7's "second admin
    /// account from an existing admin" invite, same mechanics as SendStoreOwnerInvitationEmailAsync.</summary>
    Task SendAdminInvitationEmailAsync(string toEmail, string code, CancellationToken cancellationToken);
}

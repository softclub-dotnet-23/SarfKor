namespace Application.Abstractions;

public interface IEmailSender
{
    /// <summary>Takes the raw reset token, not a URL — building the link is a config/Infrastructure concern.</summary>
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken);

    /// <summary>Takes the raw invite token, not a URL — same reasoning as the reset email.</summary>
    Task SendStoreEmployeeInviteEmailAsync(string toEmail, string storeName, string inviteToken, CancellationToken cancellationToken);
}

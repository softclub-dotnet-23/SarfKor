namespace Application.Abstractions;

public interface IEmailSender
{
    /// <summary>Takes the raw reset token, not a URL — building the link is a config/Infrastructure concern.</summary>
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken);
}

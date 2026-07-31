using Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Infrastructure.Email;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";

        return SendAsync(
            toEmail,
            "Восстановление пароля — Sarfkor",
            $"""
            <p>Вы (или кто-то другой) запросили сброс пароля для аккаунта Sarfkor.</p>
            <p><a href="{resetLink}">Нажмите здесь, чтобы задать новый пароль</a></p>
            <p>Ссылка действительна в течение 1 часа. Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>
            """,
            cancellationToken);
    }

    public Task SendStoreEmployeeInviteEmailAsync(string toEmail, string storeName, string inviteToken, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var acceptLink = $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(inviteToken)}";

        return SendAsync(
            toEmail,
            "Приглашение в команду — Sarfkor",
            $"""
            <p>Вас пригласили присоединиться к магазину «{storeName}» в Sarfkor.</p>
            <p><a href="{acceptLink}">Нажмите здесь, чтобы задать пароль и начать работу</a></p>
            <p>Ссылка действительна в течение 24 часов. Если вы не ожидали этого приглашения, просто проигнорируйте это письмо.</p>
            """,
            cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var fromName = configuration["Smtp:FromName"] ?? "Sarfkor";
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var host = configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(configuration["Smtp:Port"] ?? "587");

        // A clear, specific failure here matters: callers catch and log this (not rethrow it), so a
        // missing credential must not surface as some opaque MimeKit ArgumentNullException in the
        // logs — that's much harder to root-cause on a fresh deploy.
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Smtp:Username / Smtp:Password are not configured — set them via User Secrets (dev) or Smtp__Username / Smtp__Password environment variables (prod).");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, username));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

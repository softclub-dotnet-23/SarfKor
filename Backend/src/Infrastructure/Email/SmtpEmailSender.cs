using Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace Infrastructure.Email;

public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetCodeAsync(string toEmail, string code, CancellationToken cancellationToken) =>
        SendAsync(
            toEmail,
            "Восстановление пароля — Sarfkor",
            $"""
            <p>Код для сброса пароля аккаунта Sarfkor: <strong>{code}</strong></p>
            <p>Код действителен в течение 15 минут. Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>
            """,
            cancellationToken);

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

    public Task SendStoreOwnerInvitationEmailAsync(string toEmail, string storeName, string code, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var confirmLink = $"{baseUrl}/confirm-store-owner?email={Uri.EscapeDataString(toEmail)}";

        return SendAsync(
            toEmail,
            "Подтверждение владельца магазина — Sarfkor",
            $"""
            <p>Администратор Sarfkor добавил вас как владельца магазина «{storeName}».</p>
            <p>Код подтверждения: <strong>{code}</strong></p>
            <p><a href="{confirmLink}">Перейдите сюда</a>, введите код и задайте пароль, чтобы начать работу.</p>
            <p>Код действителен в течение 20 минут. Если вы не ожидали этого письма, просто проигнорируйте его.</p>
            """,
            cancellationToken);
    }

    public Task SendEmailConfirmationCodeAsync(string toEmail, string code, CancellationToken cancellationToken) =>
        SendAsync(
            toEmail,
            "Подтверждение email — Sarfkor",
            $"""
            <p>Код подтверждения для регистрации в Sarfkor: <strong>{code}</strong></p>
            <p>Код действителен в течение 15 минут. Если вы не регистрировались в Sarfkor, просто проигнорируйте это письмо.</p>
            """,
            cancellationToken);

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var fromName = configuration["Smtp:FromName"] ?? "Sarfkor";
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var host = configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(configuration["Smtp:Port"] ?? "587");

        // No SMTP configured (e.g. local dev with no mail account set up) — log the email instead
        // of failing outright, so OTP-gated flows (registration, store-owner invite) stay testable
        // without real mail infrastructure. Callers still get a clean success either way.
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Smtp:Username/Smtp:Password not configured — logging email instead of sending it.\nTo: {ToEmail}\nSubject: {Subject}\nBody: {Body}",
                toEmail, subject, htmlBody);
            return;
        }

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

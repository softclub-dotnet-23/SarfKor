using Application.Abstractions;
using Domain.Stores;
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

    // Role/expiry/"ignore if unexpected" copy in both languages — see StoreEmployeeInvitation
    // task's spec: "на языке приглашающего магазина" (in practice, the inviting owner's own
    // UserProfile.PreferredLanguage, the closest concept this domain actually has to "магазина
    // язык" — there is no separate per-store language setting).
    private static readonly Dictionary<StoreEmployeeRole, (string Ru, string Tg)> RoleNames = new()
    {
        [StoreEmployeeRole.Cashier] = ("Кассир", "Кассир"),
        [StoreEmployeeRole.Owner] = ("Владелец", "Соҳиб"),
    };

    public Task SendStoreEmployeeInviteEmailAsync(
        string toEmail, string storeName, StoreEmployeeRole role, string inviteToken, int expiryDays, string language, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var acceptLink = $"{baseUrl}/invite/{Uri.EscapeDataString(inviteToken)}";
        var roleName = RoleNames.TryGetValue(role, out var names) ? (language == "tg" ? names.Tg : names.Ru) : role.ToString();

        var (subject, body) = language == "tg"
            ? (
                "Даъват ба даста — Sarfkor",
                $"""
                <p>Шуморо даъват карданд, ки ба мағозаи «{storeName}» дар Sarfkor ҳамроҳ шавед — нақш: <strong>{roleName}</strong>.</p>
                <p><a href="{acceptLink}">Инҷо зер кунед, то дохил шавед ва кор оғоз кунед</a></p>
                <p>Истинод {expiryDays} рӯз эътибор дорад. Агар шумо ин даъватро интизор набудед, танҳо ин номаро нодида гиред.</p>
                """
              )
            : (
                "Приглашение в команду — Sarfkor",
                $"""
                <p>Вас пригласили присоединиться к магазину «{storeName}» в Sarfkor — роль: <strong>{roleName}</strong>.</p>
                <p><a href="{acceptLink}">Нажмите здесь, чтобы войти и начать работу</a></p>
                <p>Ссылка действительна {expiryDays} дн. Если вы не ожидали этого приглашения, просто проигнорируйте это письмо.</p>
                """
              );

        return SendAsync(toEmail, subject, body, cancellationToken);
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

    public Task SendAdminInvitationEmailAsync(string toEmail, string code, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var confirmLink = $"{baseUrl}/confirm-admin-invite?email={Uri.EscapeDataString(toEmail)}";

        return SendAsync(
            toEmail,
            "Приглашение администратора — Sarfkor",
            $"""
            <p>Вас пригласили как администратора платформы Sarfkor.</p>
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

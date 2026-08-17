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
    private static readonly Dictionary<StoreEmployeeRole, (string Ru, string Tg)> EmployeeRoleNames = new()
    {
        [StoreEmployeeRole.Cashier] = ("Кассир", "Кассир"),
        [StoreEmployeeRole.Owner] = ("Владелец", "Соҳиб"),
    };

    // Same idea, for the three top-level Identity roles /admin/users can invite someone into —
    // StorePartner already has a role name from EmployeeRoleNames above (Owner/Cashier), so it's
    // deliberately absent here; see the RoleName local function below.
    private static readonly Dictionary<string, (string Ru, string Tg)> IdentityRoleNames = new()
    {
        ["User"] = ("Пользователь", "Истифодабаранда"),
        ["Admin"] = ("Администратор", "Маъмур"),
    };

    public Task SendInvitationEmailAsync(
        string toEmail, string invitedRole, string? storeName, StoreEmployeeRole? employeeRole,
        string inviteToken, int expiryDays, string language, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var acceptLink = $"{baseUrl}/invite/{Uri.EscapeDataString(inviteToken)}";

        string RoleName() =>
            employeeRole is { } role
                ? (EmployeeRoleNames.TryGetValue(role, out var employeeNames) ? (language == "tg" ? employeeNames.Tg : employeeNames.Ru) : role.ToString())
                : (IdentityRoleNames.TryGetValue(invitedRole, out var identityNames) ? (language == "tg" ? identityNames.Tg : identityNames.Ru) : invitedRole);

        var roleName = RoleName();
        // "join store «X»" only makes sense when there is one — a plain User/Admin invite has no
        // store at all, so the sentence drops that clause entirely rather than saying "join «»".
        var (subject, body) = language == "tg"
            ? (
                "Даъват ба Sarfkor",
                storeName is not null
                    ? $"""
                       <p>Шуморо даъват карданд, ки ба мағозаи «{storeName}» дар Sarfkor ҳамроҳ шавед — нақш: <strong>{roleName}</strong>.</p>
                       <p><a href="{acceptLink}">Инҷо зер кунед, то дохил шавед ва кор оғоз кунед</a></p>
                       <p>Истинод {expiryDays} рӯз эътибор дорад. Агар шумо ин даъватро интизор набудед, танҳо ин номаро нодида гиред.</p>
                       """
                    : $"""
                       <p>Шуморо даъват карданд, ки ба платформаи Sarfkor ҳамроҳ шавед — нақш: <strong>{roleName}</strong>.</p>
                       <p><a href="{acceptLink}">Инҷо зер кунед, то дохил шавед ва кор оғоз кунед</a></p>
                       <p>Истинод {expiryDays} рӯз эътибор дорад. Агар шумо ин даъватро интизор набудед, танҳо ин номаро нодида гиред.</p>
                       """
              )
            : (
                "Приглашение в Sarfkor",
                storeName is not null
                    ? $"""
                       <p>Вас пригласили присоединиться к магазину «{storeName}» в Sarfkor — роль: <strong>{roleName}</strong>.</p>
                       <p><a href="{acceptLink}">Нажмите здесь, чтобы войти и начать работу</a></p>
                       <p>Ссылка действительна {expiryDays} дн. Если вы не ожидали этого приглашения, просто проигнорируйте это письмо.</p>
                       """
                    : $"""
                       <p>Вас пригласили присоединиться к платформе Sarfkor — роль: <strong>{roleName}</strong>.</p>
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

        // MailKit's own Timeout (milliseconds, socket-level) is the belt to cancellationToken's
        // braces: a CT can end up cancelling late depending on exactly where a connect/DNS/TLS
        // handshake stalls, but SmtpClient.Timeout cuts every blocking socket operation at a fixed
        // point no matter what. Confirmed live on Railway: SMTP to smtp.gmail.com can hang minutes
        // with neither of these set (see WORKLOG) — this is the fix, on top of no longer running
        // inside the HTTP request at all (see QueuedEmailSender/EmailSenderBackgroundService).
        using var client = new SmtpClient { Timeout = 8000 };
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

using Application.Abstractions;
using Domain.Stores;

namespace Infrastructure.Email;

/// <summary>The IEmailSender every Application handler actually calls. Every method just enqueues the
/// real send onto EmailJobQueue and returns immediately — "await emailSender.SendXxxAsync(...)"
/// inside an HTTP request no longer blocks on SMTP, ever, no matter how slow or unreachable the mail
/// server is. EmailSenderBackgroundService is what actually talks to smtp.gmail.com, off the request
/// thread, with its own timeout. Zero call sites elsewhere in the codebase needed to change for this —
/// that's the point of keeping the same interface.</summary>
public sealed class QueuedEmailSender(EmailJobQueue queue) : IEmailSender
{
    public Task SendPasswordResetCodeAsync(string toEmail, string code, CancellationToken cancellationToken)
    {
        queue.Enqueue((sender, ct) => sender.SendPasswordResetCodeAsync(toEmail, code, ct));
        return Task.CompletedTask;
    }

    public Task SendInvitationEmailAsync(
        string toEmail, string invitedRole, string? storeName, StoreEmployeeRole? employeeRole,
        string inviteToken, int expiryDays, string language, CancellationToken cancellationToken)
    {
        queue.Enqueue((sender, ct) => sender.SendInvitationEmailAsync(toEmail, invitedRole, storeName, employeeRole, inviteToken, expiryDays, language, ct));
        return Task.CompletedTask;
    }

    public Task SendStoreOwnerInvitationEmailAsync(string toEmail, string storeName, string code, CancellationToken cancellationToken)
    {
        queue.Enqueue((sender, ct) => sender.SendStoreOwnerInvitationEmailAsync(toEmail, storeName, code, ct));
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationCodeAsync(string toEmail, string code, CancellationToken cancellationToken)
    {
        queue.Enqueue((sender, ct) => sender.SendEmailConfirmationCodeAsync(toEmail, code, ct));
        return Task.CompletedTask;
    }

    public Task SendAdminInvitationEmailAsync(string toEmail, string code, CancellationToken cancellationToken)
    {
        queue.Enqueue((sender, ct) => sender.SendAdminInvitationEmailAsync(toEmail, code, ct));
        return Task.CompletedTask;
    }
}

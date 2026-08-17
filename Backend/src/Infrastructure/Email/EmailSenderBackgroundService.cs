using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

/// <summary>Drains EmailJobQueue and executes each send against the real SmtpEmailSender, off the
/// request thread — this is what makes QueuedEmailSender's "return immediately" promise safe. Each
/// job gets a fresh DI scope (SmtpEmailSender only needs IConfiguration/ILogger today, both
/// scope-agnostic, but this stays scope-correct in case that ever changes) and a hard timeout on top
/// of SmtpEmailSender's own MailKit-level Timeout, so one slow/unreachable SMTP server can never hang
/// this service or pile up unboundedly. One failed send is logged and skipped — never crashes the
/// service, which would silently stop every future email until the process restarts.</summary>
public sealed class EmailSenderBackgroundService(
    EmailJobQueue queue, IServiceScopeFactory scopeFactory, ILogger<EmailSenderBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                await job(sender, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background email send failed");
            }
        }
    }
}

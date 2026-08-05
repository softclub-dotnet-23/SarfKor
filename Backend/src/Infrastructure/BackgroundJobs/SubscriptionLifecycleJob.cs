using Application.Abstractions;
using Application.Reputation;
using Application.Subscriptions;
using Domain.Auditing;
using Domain.Stores;
using Domain.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Daily pass over every StoreSubscription (ADMIN_PROMPT.md §2.1: "Ежедневная фоновая задача:
/// Trial/Active с истёкшей датой → PastDue; PastDue дольше льготного периода → Suspended") plus the
/// ContributorTrustScore decay (§2.4: "давние события весят меньше свежих"). One job for both,
/// not two, since both are "walk every row once a day" work with no reason to run on different
/// schedules — splitting them would just double the boilerplate.
///
/// A new DI scope is created per run (BackgroundService itself is a singleton, but AppDbContext and
/// every repository are scoped) — the standard pattern for a hosted service that needs scoped
/// dependencies.
/// </summary>
public sealed class SubscriptionLifecycleJob(
    IServiceScopeFactory scopeFactory,
    IOptions<SubscriptionOptions> subscriptionOptions,
    ILogger<SubscriptionLifecycleJob> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A short initial delay, not zero — lets migrations/seeding in Program.cs finish first
        // (this job and that startup code can otherwise race against the same tables on a very
        // fresh database).
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Never let one bad run kill the loop — same day's transitions just get picked up
                // on the next tick, and a fresh scope means one run's failed change tracker state
                // can't leak into the next.
                logger.LogError(ex, "SubscriptionLifecycleJob run failed");
            }

            try
            {
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var storeSubscriptionRepository = scope.ServiceProvider.GetRequiredService<IStoreSubscriptionRepository>();
        var auditLogRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var trustScoreRepository = scope.ServiceProvider.GetRequiredService<IContributorTrustScoreRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTimeOffset.UtcNow;
        var gracePeriodDays = subscriptionOptions.Value.PastDueGracePeriodDays;

        var toPastDue = await storeSubscriptionRepository.GetEndingBeforeAsync(now, cancellationToken);
        foreach (var subscription in toPastDue)
        {
            if (subscription.Status is not (SubscriptionStatus.Trial or SubscriptionStatus.Active))
                continue;

            var previousStatus = subscription.Status;
            subscription.Status = SubscriptionStatus.PastDue;

            auditLogRepository.Add(new AuditLog
            {
                PerformedByUserId = "system",
                Action = "StoreSubscription.PastDue",
                EntityType = nameof(StoreSubscription),
                EntityId = subscription.Id,
                Details = $"Automatic: period ended {subscription.CurrentPeriodEndsAt:O}",
                BeforeStateJson = $$"""{"status":"{{previousStatus}}"}""",
                AfterStateJson = """{"status":"PastDue"}""",
                OccurredAt = now
            });
        }

        var toSuspend = await storeSubscriptionRepository.GetPastDueOlderThanAsync(now.AddDays(-gracePeriodDays), cancellationToken);
        foreach (var subscription in toSuspend)
        {
            subscription.Status = SubscriptionStatus.Suspended;

            auditLogRepository.Add(new AuditLog
            {
                PerformedByUserId = "system",
                Action = "StoreSubscription.Suspended",
                EntityType = nameof(StoreSubscription),
                EntityId = subscription.Id,
                Details = $"Automatic: PastDue for more than {gracePeriodDays} days",
                BeforeStateJson = """{"status":"PastDue"}""",
                AfterStateJson = """{"status":"Suspended"}""",
                OccurredAt = now
            });
        }

        // Decay every score toward the neutral default — see TrustScoreFormula for why this, not a
        // reprocessing of historical events, is what "давние события весят меньше свежих" means here.
        var allScores = await trustScoreRepository.GetAllForDecayAsync(cancellationToken);
        foreach (var score in allScores)
        {
            score.Score = TrustScoreFormula.ApplyDailyDecay(score.Score);
            score.UpdatedAt = now;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (toPastDue.Count > 0 || toSuspend.Count > 0)
            logger.LogInformation(
                "SubscriptionLifecycleJob: {PastDueCount} subscriptions moved to PastDue, {SuspendedCount} moved to Suspended, {DecayedCount} trust scores decayed",
                toPastDue.Count(s => s.Status == SubscriptionStatus.PastDue), toSuspend.Count, allScores.Count);
    }
}

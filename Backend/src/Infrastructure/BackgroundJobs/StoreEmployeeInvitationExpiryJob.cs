using Application.Abstractions;
using Domain.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>Sweeps Pending StoreEmployeeInvitation rows past their ExpiresAt and marks them
/// Expired (task spec: "Просроченные приглашения помечать фоновой задачей"). The public
/// GetStoreEmployeeInvitationByTokenQuery/GetStoreEmployeeInvitationsQuery already compute the
/// *effective* status live (IsEffectivelyExpired) so a stale row never LOOKS pending to a caller
/// even between sweeps — this job just makes the stored Status column eventually consistent with
/// that, so a plain `WHERE Status = 'Pending'` query elsewhere also stays correct.</summary>
public sealed class StoreEmployeeInvitationExpiryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<StoreEmployeeInvitationExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                logger.LogError(ex, "StoreEmployeeInvitationExpiryJob run failed");
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
        var invitationRepository = scope.ServiceProvider.GetRequiredService<IStoreEmployeeInvitationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTimeOffset.UtcNow;
        var expired = await invitationRepository.GetPendingExpiredAsync(now, cancellationToken);
        if (expired.Count == 0)
            return;

        foreach (var invitation in expired)
            invitation.Status = StoreEmployeeInvitationStatus.Expired;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("StoreEmployeeInvitationExpiryJob: {Count} invitations marked Expired", expired.Count);
    }
}

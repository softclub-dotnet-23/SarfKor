using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Subscriptions;

namespace Application.Subscriptions.Commands.CancelStoreSubscription;

public sealed class CancelStoreSubscriptionCommandHandler(
    IStoreSubscriptionRepository storeSubscriptionRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CancelStoreSubscriptionCommand, CancelStoreSubscriptionResult>
{
    public async Task<CancelStoreSubscriptionResult> Handle(CancelStoreSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await storeSubscriptionRepository.GetByIdAsync(command.StoreSubscriptionId, cancellationToken);
        if (subscription is null)
            return new CancelStoreSubscriptionResult(CancelStoreSubscriptionOutcome.NotFound);

        if (subscription.Status == SubscriptionStatus.Cancelled)
            return new CancelStoreSubscriptionResult(CancelStoreSubscriptionOutcome.AlreadyCancelled);

        var previousStatus = subscription.Status;
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.Note = command.Reason;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "StoreSubscription.Cancelled",
            EntityType = nameof(StoreSubscription),
            EntityId = subscription.Id,
            Reason = command.Reason,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = $$"""{"status":"{{previousStatus}}"}""",
            AfterStateJson = """{"status":"Cancelled"}""",
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CancelStoreSubscriptionResult(CancelStoreSubscriptionOutcome.Cancelled);
    }
}

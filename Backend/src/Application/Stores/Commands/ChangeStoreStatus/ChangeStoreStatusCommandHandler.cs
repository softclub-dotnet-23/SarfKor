using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.ChangeStoreStatus;

public sealed class ChangeStoreStatusCommandHandler(
    IStoreRepository storeRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeStoreStatusCommand, ChangeStoreStatusResult>
{
    // Archived and Rejected are terminal — no transition out of either exists here (ADMIN_PROMPT.md
    // §2.2: Archived access is closed "навсегда"). Approve (PendingApproval → Active) is deliberately
    // absent — that's ApproveStoreCommand, which also issues a Trial subscription.
    private static readonly Dictionary<StoreStatus, StoreStatus[]> LegalTransitions = new()
    {
        [StoreStatus.PendingApproval] = [StoreStatus.Rejected],
        [StoreStatus.Active] = [StoreStatus.Suspended, StoreStatus.Blocked, StoreStatus.Archived],
        [StoreStatus.Suspended] = [StoreStatus.Active, StoreStatus.Blocked, StoreStatus.Archived],
        [StoreStatus.Blocked] = [StoreStatus.Active, StoreStatus.Archived],
    };

    public async Task<ChangeStoreStatusResult> Handle(ChangeStoreStatusCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new ChangeStoreStatusResult(ChangeStoreStatusOutcome.NotFound);

        if (!LegalTransitions.TryGetValue(store.Status, out var allowed) || !allowed.Contains(command.NewStatus))
            return new ChangeStoreStatusResult(ChangeStoreStatusOutcome.IllegalTransition);

        var previousStatus = store.Status;
        store.Status = command.NewStatus;
        store.StatusReason = command.Reason;
        store.StatusChangedAt = DateTimeOffset.UtcNow;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = $"Store.{command.NewStatus}",
            EntityType = nameof(Store),
            EntityId = store.Id,
            Reason = command.Reason,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = $$"""{"status":"{{previousStatus}}"}""",
            AfterStateJson = $$"""{"status":"{{command.NewStatus}}"}""",
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChangeStoreStatusResult(ChangeStoreStatusOutcome.Changed);
    }
}

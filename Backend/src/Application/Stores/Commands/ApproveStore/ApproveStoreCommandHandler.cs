using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.ApproveStore;

public sealed class ApproveStoreCommandHandler(
    IStoreRepository storeRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveStoreCommand, ApproveStoreResult>
{
    public async Task<ApproveStoreResult> Handle(ApproveStoreCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new ApproveStoreResult(ApproveStoreOutcome.NotFound);

        if (store.Status == StoreStatus.Approved)
            return new ApproveStoreResult(ApproveStoreOutcome.AlreadyApproved);

        // No IStoreAccessAuthorizer check here on purpose — this is an Admin action, not an
        // ownership check, and an Admin approving their own store would defeat the point of it.
        store.Status = StoreStatus.Approved;

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "Store.Approved",
            EntityType = nameof(Store),
            EntityId = store.Id,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApproveStoreResult(ApproveStoreOutcome.Approved);
    }
}

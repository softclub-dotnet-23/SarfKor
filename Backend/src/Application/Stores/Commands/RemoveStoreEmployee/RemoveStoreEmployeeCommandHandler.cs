using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Commands.RemoveStoreEmployee;

public sealed class RemoveStoreEmployeeCommandHandler(
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreRepository storeRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveStoreEmployeeCommand, RemoveStoreEmployeeResult>
{
    private const string StorePartnerRole = "StorePartner";

    public async Task<RemoveStoreEmployeeResult> Handle(RemoveStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await storeEmployeeRepository.GetByIdAsync(command.StoreEmployeeId, cancellationToken);
        if (employee is null)
            return new RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome.NotFound);

        var store = await storeRepository.GetByIdAsync(employee.StoreId, cancellationToken);
        if (store is null || store.OwnerUserId != command.PerformedByUserId)
            return new RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome.Forbidden);

        var removedUserId = employee.UserId;
        storeEmployeeRepository.Remove(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // AddStoreEmployeeCommandHandler grants StorePartner on hire so the [Authorize("StorePartner")]
        // gate on POS/inventory controllers doesn't reject a legitimate cashier before their
        // fine-grained "owner or employee of this store" check ever runs. Mirror that on removal —
        // but only if this store was their last one, since they may still own or work at others.
        var stillNeedsRole = await storeRepository.OwnsAnyStoreAsync(removedUserId, cancellationToken)
            || await storeEmployeeRepository.IsEmployedAnywhereAsync(removedUserId, cancellationToken);
        if (!stillNeedsRole)
            await authService.RemoveFromRoleAsync(removedUserId, StorePartnerRole, cancellationToken);

        return new RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome.Removed);
    }
}

using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Commands.RemoveStoreEmployee;

public sealed class RemoveStoreEmployeeCommandHandler(
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveStoreEmployeeCommand, RemoveStoreEmployeeResult>
{
    public async Task<RemoveStoreEmployeeResult> Handle(RemoveStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await storeEmployeeRepository.GetByIdAsync(command.StoreEmployeeId, cancellationToken);
        if (employee is null)
            return new RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome.NotFound);

        var store = await storeRepository.GetByIdAsync(employee.StoreId, cancellationToken);
        if (store is null || store.OwnerUserId != command.PerformedByUserId)
            return new RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome.Forbidden);

        storeEmployeeRepository.Remove(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RemoveStoreEmployeeResult(RemoveStoreEmployeeOutcome.Removed);
    }
}

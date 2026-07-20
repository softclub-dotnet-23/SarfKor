using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed class AddStoreEmployeeCommandHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<AddStoreEmployeeCommand, AddStoreEmployeeResult>
{
    public async Task<AddStoreEmployeeResult> Handle(AddStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Forbidden, null);

        var existing = await storeEmployeeRepository.GetByStoreIdAsync(command.StoreId, cancellationToken);
        if (existing.Any(e => e.UserId == command.EmployeeUserId))
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.AlreadyEmployed, null);

        var employee = new StoreEmployee
        {
            StoreId = command.StoreId,
            UserId = command.EmployeeUserId,
            Role = command.Role,
            AddedAt = DateTimeOffset.UtcNow
        };

        storeEmployeeRepository.Add(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Added, employee.Id);
    }
}

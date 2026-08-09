using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Stores;

namespace Application.Stores.Commands.SetStoreEmployeeActive;

public sealed class SetStoreEmployeeActiveCommandHandler(
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SetStoreEmployeeActiveCommand, SetStoreEmployeeActiveResult>
{
    public async Task<SetStoreEmployeeActiveResult> Handle(SetStoreEmployeeActiveCommand command, CancellationToken cancellationToken)
    {
        var employee = await storeEmployeeRepository.GetByIdAsync(command.StoreEmployeeId, cancellationToken);
        if (employee is null)
            return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(employee.StoreId, command.PerformedByUserId, cancellationToken))
            return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.Forbidden);

        employee.IsActive = command.IsActive;
        storeEmployeeRepository.Update(employee);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = command.IsActive ? "CashierAccount.Enabled" : "CashierAccount.Disabled",
            EntityType = nameof(StoreEmployee),
            EntityId = employee.Id,
            Details = $"{(command.IsActive ? "Enabled" : "Disabled")} {employee.FirstName} {employee.LastName} (store {employee.StoreId})",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetStoreEmployeeActiveResult(SetStoreEmployeeActiveOutcome.Updated);
    }
}

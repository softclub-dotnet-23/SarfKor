using Application.Abstractions;
using Application.Common;
using Domain.Stores;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed class AddStoreEmployeeCommandHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork) : ICommandHandler<AddStoreEmployeeCommand, AddStoreEmployeeResult>
{
    private const string StorePartnerRole = "StorePartner";

    public async Task<AddStoreEmployeeResult> Handle(AddStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Forbidden, null);

        // The owner only knows the cashier's email (there is no user directory to browse) — resolve
        // it to the real UserId here, since that's what StoreEmployee/JWT claims actually key on.
        var employeeUserId = await authService.FindUserIdByEmailAsync(command.EmployeeEmail, cancellationToken);
        if (employeeUserId is null)
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.EmployeeNotFound, null);

        var existing = await storeEmployeeRepository.GetByStoreIdAsync(command.StoreId, cancellationToken);
        if (existing.Any(e => e.UserId == employeeUserId))
            return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.AlreadyEmployed, null);

        var employee = new StoreEmployee
        {
            StoreId = command.StoreId,
            UserId = employeeUserId,
            Role = command.Role,
            AddedAt = DateTimeOffset.UtcNow
        };

        storeEmployeeRepository.Add(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // [Authorize("StorePartner")] on every POS/inventory controller checks this coarse JWT role
        // claim before a request ever reaches a handler's fine-grained "owner or employee of *this*
        // store" check — without it, a newly-added cashier would be rejected at the attribute gate
        // and never even reach the use-case-level authorization this StoreEmployee row exists for.
        await authService.AssignRoleAsync(employeeUserId, StorePartnerRole, cancellationToken);

        return new AddStoreEmployeeResult(AddStoreEmployeeOutcome.Added, employee.Id);
    }
}

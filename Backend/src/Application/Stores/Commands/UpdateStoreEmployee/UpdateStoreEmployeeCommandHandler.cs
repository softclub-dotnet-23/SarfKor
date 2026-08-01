using Application.Abstractions;
using Application.Common;
using Domain.ValueObjects;

namespace Application.Stores.Commands.UpdateStoreEmployee;

public sealed class UpdateStoreEmployeeCommandHandler(
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateStoreEmployeeCommand, UpdateStoreEmployeeResult>
{
    public async Task<UpdateStoreEmployeeResult> Handle(UpdateStoreEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await storeEmployeeRepository.GetByIdAsync(command.StoreEmployeeId, cancellationToken);
        if (employee is null)
            return new UpdateStoreEmployeeResult(UpdateStoreEmployeeOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(employee.StoreId, command.PerformedByUserId, cancellationToken))
            return new UpdateStoreEmployeeResult(UpdateStoreEmployeeOutcome.Forbidden);

        employee.MonthlySalary = command.MonthlySalaryAmount is null
            ? null
            : new Money(command.MonthlySalaryAmount.Value, command.MonthlySalaryCurrency!);
        employee.ScheduleStart = command.ScheduleStart;
        employee.ScheduleEnd = command.ScheduleEnd;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateStoreEmployeeResult(UpdateStoreEmployeeOutcome.Updated);
    }
}

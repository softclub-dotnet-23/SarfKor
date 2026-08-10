using Application.Abstractions;
using Application.Common;
using Domain.Sales;
using Domain.ValueObjects;

namespace Application.Sales.Commands.OpenCashierShift;

public sealed class OpenCashierShiftCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ICashierShiftRepository cashierShiftRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<OpenCashierShiftCommand, OpenCashierShiftResult>
{
    public async Task<OpenCashierShiftResult> Handle(OpenCashierShiftCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new OpenCashierShiftResult(OpenCashierShiftOutcome.StoreNotFound, null);

        // The owner or any employee added via AddStoreEmployeeCommand (CLAUDE.md §9 cashier
        // sub-role) can open a shift for this store.
        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new OpenCashierShiftResult(OpenCashierShiftOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new OpenCashierShiftResult(OpenCashierShiftOutcome.SubscriptionInactive, null);

        var shift = new CashierShift
        {
            StoreId = command.StoreId,
            CashierUserId = command.PerformedByUserId,
            OpeningCash = new Money(command.OpeningCash, command.Currency),
            StartedAt = DateTimeOffset.UtcNow
        };

        cashierShiftRepository.Add(shift);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new OpenCashierShiftResult(OpenCashierShiftOutcome.Opened, shift.Id);
    }
}

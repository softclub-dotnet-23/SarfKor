using Application.Abstractions;
using Application.Common;
using Domain.Sales;
using Domain.ValueObjects;

namespace Application.Sales.Commands.OpenCashierShift;

public sealed class OpenCashierShiftCommandHandler(
    IStoreRepository storeRepository,
    ICashierShiftRepository cashierShiftRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<OpenCashierShiftCommand, OpenCashierShiftResult>
{
    public async Task<OpenCashierShiftResult> Handle(OpenCashierShiftCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new OpenCashierShiftResult(OpenCashierShiftOutcome.StoreNotFound, null);

        // No separate "cashier" sub-role exists yet (CLAUDE.md §9 open question) — for now only
        // the store owner can open a shift for their own store, acting as their own cashier.
        if (store.OwnerUserId != command.PerformedByUserId)
            return new OpenCashierShiftResult(OpenCashierShiftOutcome.Forbidden, null);

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

using Application.Abstractions;
using Application.Common;
using Domain.Sales;
using Domain.ValueObjects;

namespace Application.Sales.Commands.CloseCashierShift;

/// <summary>
/// CLAUDE.md §10: "рассмотреть периодическую сверку остатка с фактическим" — this is that
/// reconciliation applied to cash instead of stock. ExpectedCash is computed, never trusted from
/// the client, so a cashier can't just type in whatever number makes the shift look balanced.
/// </summary>
public sealed class CloseCashierShiftCommandHandler(
    ICashierShiftRepository cashierShiftRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISaleTransactionRepository saleTransactionRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CloseCashierShiftCommand, CloseCashierShiftResult>
{
    public async Task<CloseCashierShiftResult> Handle(CloseCashierShiftCommand command, CancellationToken cancellationToken)
    {
        var shift = await cashierShiftRepository.GetByIdAsync(command.CashierShiftId, cancellationToken);
        if (shift is null)
            return new CloseCashierShiftResult(CloseCashierShiftOutcome.NotFound, null, null, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(shift.StoreId, command.PerformedByUserId, cancellationToken))
            return new CloseCashierShiftResult(CloseCashierShiftOutcome.Forbidden, null, null, null);

        if (shift.EndedAt is not null)
            return new CloseCashierShiftResult(CloseCashierShiftOutcome.AlreadyClosed, null, null, null);

        var sales = await saleTransactionRepository.GetAllInRangeAsync(shift.StoreId, shift.StartedAt, DateTimeOffset.UtcNow, cancellationToken);
        var cashSalesRevenue = sales
            .Where(s => s.CashierUserId == shift.CashierUserId && s.Status == SaleStatus.Completed)
            .SelectMany(s => s.Lines)
            .Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);

        var expectedCash = shift.OpeningCash.Amount + cashSalesRevenue;

        shift.ExpectedCash = new Money(expectedCash, shift.OpeningCash.Currency);
        shift.ClosingCash = new Money(command.ClosingCash, shift.OpeningCash.Currency);
        shift.EndedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CloseCashierShiftResult(CloseCashierShiftOutcome.Closed, expectedCash, command.ClosingCash, command.ClosingCash - expectedCash);
    }
}

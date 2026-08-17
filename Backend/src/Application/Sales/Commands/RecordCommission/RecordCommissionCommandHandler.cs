using Application.Abstractions;
using Application.Common;
using Domain.Sales;
using Domain.ValueObjects;

namespace Application.Sales.Commands.RecordCommission;

public sealed class RecordCommissionCommandHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ICommissionRepository commissionRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordCommissionCommand, RecordCommissionResult>
{
    public async Task<RecordCommissionResult> Handle(RecordCommissionCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleTransactionRepository.GetByIdAsync(command.SaleTransactionId, cancellationToken);
        if (sale is null)
            return new RecordCommissionResult(RecordCommissionOutcome.SaleNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(sale.StoreId, command.PerformedByUserId, cancellationToken))
            return new RecordCommissionResult(RecordCommissionOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(sale.StoreId, cancellationToken))
            return new RecordCommissionResult(RecordCommissionOutcome.SubscriptionInactive, null);

        // CashierUserId comes from the sale itself, not the caller — the commission always
        // belongs to whoever actually rang up the sale, not whoever happens to be recording it.
        var commission = new Commission
        {
            SaleTransactionId = command.SaleTransactionId,
            CashierUserId = sale.CashierUserId,
            Amount = new Money(command.Amount, command.Currency),
            CreatedAt = DateTimeOffset.UtcNow
        };

        commissionRepository.Add(commission);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordCommissionResult(RecordCommissionOutcome.Recorded, commission.Id);
    }
}

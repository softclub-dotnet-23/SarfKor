using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.Sales;

namespace Application.Sales.Commands.VoidSale;

public sealed class VoidSaleCommandHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<VoidSaleCommand, VoidSaleResult>
{
    public async Task<VoidSaleResult> Handle(VoidSaleCommand command, CancellationToken cancellationToken)
    {
        var saleTransaction = await saleTransactionRepository.GetByIdAsync(command.SaleTransactionId, cancellationToken);
        if (saleTransaction is null)
            return new VoidSaleResult(VoidSaleOutcome.NotFound, null);

        var store = await storeRepository.GetByIdAsync(saleTransaction.StoreId, cancellationToken);
        if (store is null)
            return new VoidSaleResult(VoidSaleOutcome.Forbidden, null);

        if (store.OwnerUserId != command.VoidedByUserId
            && !await storeEmployeeRepository.IsEmployeeAsync(store.Id, command.VoidedByUserId, cancellationToken))
            return new VoidSaleResult(VoidSaleOutcome.Forbidden, null);

        if (saleTransaction.Status == SaleStatus.Voided)
            return new VoidSaleResult(VoidSaleOutcome.AlreadyVoided, null);

        var voidedAt = DateTimeOffset.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            saleTransaction.Status = SaleStatus.Voided;
            saleTransaction.VoidedByUserId = command.VoidedByUserId;
            saleTransaction.VoidReason = command.Reason;
            saleTransaction.VoidedAt = voidedAt;

            foreach (var line in saleTransaction.Lines)
            {
                await stockLevelRepository.IncrementAsync(line.ProductId, saleTransaction.StoreId, line.Quantity, ct);

                stockMovementRepository.Add(new StockMovement
                {
                    ProductId = line.ProductId,
                    StoreId = saleTransaction.StoreId,
                    Type = StockMovementType.Correction,
                    QuantityDelta = line.Quantity,
                    Reason = $"Void of sale #{saleTransaction.Id}: {command.Reason}",
                    RelatedSaleTransactionId = saleTransaction.Id,
                    PerformedByUserId = command.VoidedByUserId,
                    OccurredAt = voidedAt
                });
            }

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new VoidSaleResult(VoidSaleOutcome.Voided, voidedAt);
    }
}

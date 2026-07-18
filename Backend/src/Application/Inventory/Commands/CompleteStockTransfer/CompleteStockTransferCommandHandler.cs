using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.CompleteStockTransfer;

public sealed class CompleteStockTransferCommandHandler(
    IStockTransferRepository stockTransferRepository,
    IStoreRepository storeRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CompleteStockTransferCommand, CompleteStockTransferResult>
{
    public async Task<CompleteStockTransferResult> Handle(CompleteStockTransferCommand command, CancellationToken cancellationToken)
    {
        var transfer = await stockTransferRepository.GetByIdAsync(command.StockTransferId, cancellationToken);
        if (transfer is null)
            return new CompleteStockTransferResult(CompleteStockTransferOutcome.NotFound);

        var toStore = await storeRepository.GetByIdAsync(transfer.ToStoreId, cancellationToken);
        if (toStore is null || toStore.OwnerUserId != command.PerformedByUserId)
            return new CompleteStockTransferResult(CompleteStockTransferOutcome.Forbidden);

        if (transfer.Status != StockTransferStatus.InTransit)
            return new CompleteStockTransferResult(CompleteStockTransferOutcome.NotInTransit);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await stockLevelRepository.IncrementAsync(transfer.ProductId, transfer.ToStoreId, transfer.Quantity, ct);

            stockMovementRepository.Add(new StockMovement
            {
                ProductId = transfer.ProductId,
                StoreId = transfer.ToStoreId,
                Type = StockMovementType.Correction,
                QuantityDelta = transfer.Quantity,
                Reason = $"Stock transfer in from store {transfer.FromStoreId}",
                PerformedByUserId = command.PerformedByUserId,
                OccurredAt = DateTimeOffset.UtcNow
            });

            transfer.Status = StockTransferStatus.Completed;
            transfer.CompletedAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new CompleteStockTransferResult(CompleteStockTransferOutcome.Completed);
    }
}

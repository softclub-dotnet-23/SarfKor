using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.RecordStockReceipt;

public sealed class RecordStockReceiptCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordStockReceiptCommand, RecordStockReceiptResult>
{
    public async Task<RecordStockReceiptResult> Handle(RecordStockReceiptCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new RecordStockReceiptResult(RecordStockReceiptOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new RecordStockReceiptResult(RecordStockReceiptOutcome.Forbidden, null);

        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return new RecordStockReceiptResult(RecordStockReceiptOutcome.ProductNotFound, null);

        var movement = new StockMovement
        {
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Type = StockMovementType.Receipt,
            QuantityDelta = command.Quantity,
            SupplierId = command.SupplierId,
            PerformedByUserId = command.PerformedByUserId,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await stockLevelRepository.IncrementAsync(command.ProductId, command.StoreId, command.Quantity, ct);
            stockMovementRepository.Add(movement);
            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new RecordStockReceiptResult(RecordStockReceiptOutcome.Received, movement.Id);
    }
}

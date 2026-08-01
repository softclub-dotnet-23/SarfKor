using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.ReceivePurchaseOrder;

public sealed class ReceivePurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReceivePurchaseOrderCommand, ReceivePurchaseOrderResult>
{
    public async Task<ReceivePurchaseOrderResult> Handle(ReceivePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetByIdAsync(command.PurchaseOrderId, cancellationToken);
        if (order is null)
            return new ReceivePurchaseOrderResult(ReceivePurchaseOrderOutcome.NotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(order.StoreId, command.PerformedByUserId, cancellationToken))
            return new ReceivePurchaseOrderResult(ReceivePurchaseOrderOutcome.Forbidden);

        if (order.Status != PurchaseOrderStatus.Submitted)
            return new ReceivePurchaseOrderResult(ReceivePurchaseOrderOutcome.NotSubmitted);

        // Receiving a PO is where the paperwork becomes real stock — same transactional pattern
        // as RecordStockReceiptCommand, just driven by the order's lines instead of a single item.
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var line in order.Lines)
            {
                await stockLevelRepository.IncrementAsync(line.ProductId, order.StoreId, line.Quantity, ct);
                stockMovementRepository.Add(new StockMovement
                {
                    ProductId = line.ProductId,
                    StoreId = order.StoreId,
                    Type = StockMovementType.Receipt,
                    QuantityDelta = line.Quantity,
                    SupplierId = order.SupplierId,
                    PerformedByUserId = command.PerformedByUserId,
                    OccurredAt = DateTimeOffset.UtcNow
                });
            }

            order.Status = PurchaseOrderStatus.Received;
            order.ReceivedAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new ReceivePurchaseOrderResult(ReceivePurchaseOrderOutcome.Received);
    }
}

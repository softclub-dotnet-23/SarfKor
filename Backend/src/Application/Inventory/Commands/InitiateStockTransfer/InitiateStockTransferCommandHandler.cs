using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.InitiateStockTransfer;

public sealed class InitiateStockTransferCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IStockTransferRepository stockTransferRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<InitiateStockTransferCommand, InitiateStockTransferResult>
{
    public async Task<InitiateStockTransferResult> Handle(InitiateStockTransferCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.FromStoreId, cancellationToken))
            return new InitiateStockTransferResult(InitiateStockTransferOutcome.FromStoreNotFound, null);

        if (!await storeRepository.ExistsAsync(command.ToStoreId, cancellationToken))
            return new InitiateStockTransferResult(InitiateStockTransferOutcome.ToStoreNotFound, null);

        // Transfers move stock between two branches of the same business — both ends must belong
        // to the requester, otherwise anyone could siphon stock out of a store they don't own.
        if (!await storeAccessAuthorizer.IsOwnerAsync(command.FromStoreId, command.PerformedByUserId, cancellationToken)
            || !await storeAccessAuthorizer.IsOwnerAsync(command.ToStoreId, command.PerformedByUserId, cancellationToken))
            return new InitiateStockTransferResult(InitiateStockTransferOutcome.Forbidden, null);

        // Stock moves out of FromStoreId and into ToStoreId -- either end being Suspended closes
        // the operation, same as every other cabinet write.
        if (!await storeAccessAuthorizer.IsOperationalAsync(command.FromStoreId, cancellationToken)
            || !await storeAccessAuthorizer.IsOperationalAsync(command.ToStoreId, cancellationToken))
            return new InitiateStockTransferResult(InitiateStockTransferOutcome.SubscriptionInactive, null);

        var transfer = new StockTransfer
        {
            ProductId = command.ProductId,
            FromStoreId = command.FromStoreId,
            ToStoreId = command.ToStoreId,
            Quantity = command.Quantity,
            InitiatedByUserId = command.PerformedByUserId,
            Status = StockTransferStatus.InTransit,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var outcome = InitiateStockTransferOutcome.Initiated;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var decremented = await stockLevelRepository.TryDecrementAsync(command.ProductId, command.FromStoreId, command.Quantity, ct);
            if (!decremented)
            {
                outcome = InitiateStockTransferOutcome.InsufficientStock;
                return;
            }

            stockMovementRepository.Add(new StockMovement
            {
                ProductId = command.ProductId,
                StoreId = command.FromStoreId,
                Type = StockMovementType.Correction,
                QuantityDelta = -command.Quantity,
                Reason = $"Stock transfer out to store {command.ToStoreId}",
                PerformedByUserId = command.PerformedByUserId,
                OccurredAt = DateTimeOffset.UtcNow
            });

            stockTransferRepository.Add(transfer);
            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return outcome == InitiateStockTransferOutcome.Initiated
            ? new InitiateStockTransferResult(outcome, transfer.Id)
            : new InitiateStockTransferResult(outcome, null);
    }
}

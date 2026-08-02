using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.Sales;

namespace Application.Sales.Commands.VoidSale;

public sealed class VoidSaleCommandHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStockLevelRepository stockLevelRepository,
    IStockMovementRepository stockMovementRepository,
    IGiftCardRepository giftCardRepository,
    IStoreCreditRepository storeCreditRepository,
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

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(saleTransaction.StoreId, command.VoidedByUserId, cancellationToken))
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

            // Refund whatever gift-card/store-credit value was applied to the original sale —
            // without this, voiding a sale paid partly by gift card or store credit permanently
            // and silently lost that value: the store kept the money, the product came back into
            // stock, and the customer's balance was never restored.
            if (saleTransaction.GiftCardId is not null && saleTransaction.GiftCardAmountApplied is > 0)
                await giftCardRepository.CreditAsync(saleTransaction.GiftCardId.Value, saleTransaction.GiftCardAmountApplied.Value, ct);

            if (saleTransaction.CustomerId is not null && saleTransaction.StoreCreditAmountApplied is > 0)
            {
                var storeCredit = await storeCreditRepository.GetByStoreAndCustomerAsync(saleTransaction.StoreId, saleTransaction.CustomerId.Value, ct);
                if (storeCredit is not null)
                    await storeCreditRepository.CreditAsync(storeCredit.Id, saleTransaction.StoreCreditAmountApplied.Value, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return new VoidSaleResult(VoidSaleOutcome.Voided, voidedAt);
    }
}

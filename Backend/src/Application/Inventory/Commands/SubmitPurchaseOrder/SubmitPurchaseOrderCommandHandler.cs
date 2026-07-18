using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.SubmitPurchaseOrder;

public sealed class SubmitPurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>
{
    public async Task<SubmitPurchaseOrderResult> Handle(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetByIdAsync(command.PurchaseOrderId, cancellationToken);
        if (order is null)
            return new SubmitPurchaseOrderResult(SubmitPurchaseOrderOutcome.NotFound);

        var store = await storeRepository.GetByIdAsync(order.StoreId, cancellationToken);
        if (store is null || store.OwnerUserId != command.PerformedByUserId)
            return new SubmitPurchaseOrderResult(SubmitPurchaseOrderOutcome.Forbidden);

        if (order.Status != PurchaseOrderStatus.Draft)
            return new SubmitPurchaseOrderResult(SubmitPurchaseOrderOutcome.NotDraft);

        order.Status = PurchaseOrderStatus.Submitted;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitPurchaseOrderResult(SubmitPurchaseOrderOutcome.Submitted);
    }
}

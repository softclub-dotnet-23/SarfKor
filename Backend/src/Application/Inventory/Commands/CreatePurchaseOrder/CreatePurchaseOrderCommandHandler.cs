using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.ValueObjects;

namespace Application.Inventory.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(
    IStoreRepository storeRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResult>
{
    public async Task<CreatePurchaseOrderResult> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreatePurchaseOrderResult(CreatePurchaseOrderOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new CreatePurchaseOrderResult(CreatePurchaseOrderOutcome.Forbidden, null);

        var order = new PurchaseOrder
        {
            StoreId = command.StoreId,
            SupplierId = command.SupplierId,
            CreatedByUserId = command.PerformedByUserId,
            Status = PurchaseOrderStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = command.Lines
                .Select(l => new PurchaseOrderLineItem { ProductId = l.ProductId, Quantity = l.Quantity, UnitCost = new Money(l.UnitCost, l.Currency) })
                .ToList()
        };

        purchaseOrderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePurchaseOrderResult(CreatePurchaseOrderOutcome.Created, order.Id);
    }
}

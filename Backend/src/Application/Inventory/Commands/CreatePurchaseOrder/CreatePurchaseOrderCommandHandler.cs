using Application.Abstractions;
using Application.Common;
using Domain.Inventory;
using Domain.ValueObjects;

namespace Application.Inventory.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IPurchaseOrderRepository purchaseOrderRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResult>
{
    public async Task<CreatePurchaseOrderResult> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new CreatePurchaseOrderResult(CreatePurchaseOrderOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreatePurchaseOrderResult(CreatePurchaseOrderOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new CreatePurchaseOrderResult(CreatePurchaseOrderOutcome.SubscriptionInactive, null);

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

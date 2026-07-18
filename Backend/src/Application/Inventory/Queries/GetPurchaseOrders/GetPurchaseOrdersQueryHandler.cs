using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetPurchaseOrders;

public sealed class GetPurchaseOrdersQueryHandler(
    IStoreRepository storeRepository,
    IPurchaseOrderRepository purchaseOrderRepository) : IQueryHandler<GetPurchaseOrdersQuery, GetPurchaseOrdersResult>
{
    public async Task<GetPurchaseOrdersResult> Handle(GetPurchaseOrdersQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetPurchaseOrdersResult(GetPurchaseOrdersOutcome.StoreNotFound, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetPurchaseOrdersResult(GetPurchaseOrdersOutcome.Forbidden, null);

        var orders = await purchaseOrderRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = orders.Select(o => new PurchaseOrderDto(o.Id, o.SupplierId, o.Status, o.CreatedAt, o.ReceivedAt)).ToList();

        return new GetPurchaseOrdersResult(GetPurchaseOrdersOutcome.Found, dtos);
    }
}

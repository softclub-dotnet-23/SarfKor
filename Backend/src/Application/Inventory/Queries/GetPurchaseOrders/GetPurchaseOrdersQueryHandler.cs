using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetPurchaseOrders;

public sealed class GetPurchaseOrdersQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IPurchaseOrderRepository purchaseOrderRepository) : IQueryHandler<GetPurchaseOrdersQuery, GetPurchaseOrdersResult>
{
    public async Task<GetPurchaseOrdersResult> Handle(GetPurchaseOrdersQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetPurchaseOrdersResult(GetPurchaseOrdersOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetPurchaseOrdersResult(GetPurchaseOrdersOutcome.Forbidden, null);

        var orders = await purchaseOrderRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = orders.Select(o => new PurchaseOrderDto(o.Id, o.SupplierId, o.Status, o.CreatedAt, o.ReceivedAt)).ToList();

        return new GetPurchaseOrdersResult(GetPurchaseOrdersOutcome.Found, dtos);
    }
}

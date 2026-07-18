using Domain.Inventory;

namespace Application.Inventory.Queries.GetPurchaseOrders;

public sealed record PurchaseOrderDto(int PurchaseOrderId, int SupplierId, PurchaseOrderStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? ReceivedAt);

public enum GetPurchaseOrdersOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetPurchaseOrdersResult(GetPurchaseOrdersOutcome Outcome, IReadOnlyList<PurchaseOrderDto>? Orders);

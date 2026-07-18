using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetStockTransfers;

public sealed class GetStockTransfersQueryHandler(
    IStoreRepository storeRepository,
    IStockTransferRepository stockTransferRepository) : IQueryHandler<GetStockTransfersQuery, GetStockTransfersResult>
{
    public async Task<GetStockTransfersResult> Handle(GetStockTransfersQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStockTransfersResult(GetStockTransfersOutcome.StoreNotFound, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetStockTransfersResult(GetStockTransfersOutcome.Forbidden, null);

        var transfers = await stockTransferRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = transfers
            .Select(t => new StockTransferDto(t.Id, t.ProductId, t.FromStoreId, t.ToStoreId, t.Quantity, t.Status, t.CreatedAt, t.CompletedAt))
            .ToList();

        return new GetStockTransfersResult(GetStockTransfersOutcome.Found, dtos);
    }
}

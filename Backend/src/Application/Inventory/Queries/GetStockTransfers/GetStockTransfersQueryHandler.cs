using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetStockTransfers;

public sealed class GetStockTransfersQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStockTransferRepository stockTransferRepository) : IQueryHandler<GetStockTransfersQuery, GetStockTransfersResult>
{
    public async Task<GetStockTransfersResult> Handle(GetStockTransfersQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStockTransfersResult(GetStockTransfersOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStockTransfersResult(GetStockTransfersOutcome.Forbidden, null);

        var transfers = await stockTransferRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = transfers
            .Select(t => new StockTransferDto(t.Id, t.ProductId, t.FromStoreId, t.ToStoreId, t.Quantity, t.Status, t.CreatedAt, t.CompletedAt))
            .ToList();

        return new GetStockTransfersResult(GetStockTransfersOutcome.Found, dtos);
    }
}

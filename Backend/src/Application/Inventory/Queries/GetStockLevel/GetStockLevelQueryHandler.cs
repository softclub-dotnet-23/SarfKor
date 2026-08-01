using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetStockLevel;

public sealed class GetStockLevelQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStockLevelRepository stockLevelRepository) : IQueryHandler<GetStockLevelQuery, GetStockLevelResult>
{
    public async Task<GetStockLevelResult> Handle(GetStockLevelQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStockLevelResult(GetStockLevelOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStockLevelResult(GetStockLevelOutcome.Forbidden, null);

        var levels = await stockLevelRepository.GetByStoreAsync(query.StoreId, cancellationToken);
        var dtos = levels.Select(l => new StockLevelDto(l.ProductId, l.Quantity)).ToList();

        return new GetStockLevelResult(GetStockLevelOutcome.Found, dtos);
    }
}

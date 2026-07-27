using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetStockLevel;

public sealed class GetStockLevelQueryHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStockLevelRepository stockLevelRepository) : IQueryHandler<GetStockLevelQuery, GetStockLevelResult>
{
    public async Task<GetStockLevelResult> Handle(GetStockLevelQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStockLevelResult(GetStockLevelOutcome.StoreNotFound, null);

        if (store.OwnerUserId != query.RequestedByUserId
            && !await storeEmployeeRepository.IsEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStockLevelResult(GetStockLevelOutcome.Forbidden, null);

        var levels = await stockLevelRepository.GetByStoreAsync(query.StoreId, cancellationToken);
        var dtos = levels.Select(l => new StockLevelDto(l.ProductId, l.Quantity)).ToList();

        return new GetStockLevelResult(GetStockLevelOutcome.Found, dtos);
    }
}

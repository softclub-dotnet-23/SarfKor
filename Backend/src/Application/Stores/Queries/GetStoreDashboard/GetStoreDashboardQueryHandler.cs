using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreDashboard;

public sealed class GetStoreDashboardQueryHandler(
    IStoreRepository storeRepository,
    ISaleTransactionRepository saleTransactionRepository,
    IStockLevelRepository stockLevelRepository) : IQueryHandler<GetStoreDashboardQuery, GetStoreDashboardResult>
{
    public async Task<GetStoreDashboardResult> Handle(GetStoreDashboardQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreDashboardResult(GetStoreDashboardOutcome.StoreNotFound, null, null, null, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetStoreDashboardResult(GetStoreDashboardOutcome.Forbidden, null, null, null, null);

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var todaySales = await saleTransactionRepository.GetCompletedInRangeAsync(query.StoreId, todayStart, todayEnd, cancellationToken);
        var todayRevenue = todaySales.SelectMany(s => s.Lines).Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
        var currency = todaySales.FirstOrDefault()?.Currency;

        var stockLevels = await stockLevelRepository.GetByStoreAsync(query.StoreId, cancellationToken);
        var productsInStock = stockLevels.Count(s => s.Quantity > 0);

        return new GetStoreDashboardResult(GetStoreDashboardOutcome.Found, todaySales.Count, todayRevenue, currency, productsInStock);
    }
}

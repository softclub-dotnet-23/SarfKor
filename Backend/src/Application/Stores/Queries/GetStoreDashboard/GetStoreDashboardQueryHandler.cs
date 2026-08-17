using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreDashboard;

public sealed class GetStoreDashboardQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISaleTransactionRepository saleTransactionRepository,
    IStockLevelRepository stockLevelRepository) : IQueryHandler<GetStoreDashboardQuery, GetStoreDashboardResult>
{
    public async Task<GetStoreDashboardResult> Handle(GetStoreDashboardQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStoreDashboardResult(GetStoreDashboardOutcome.StoreNotFound, null, null, null, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStoreDashboardResult(GetStoreDashboardOutcome.Forbidden, null, null, null, null);

        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var todaySales = await saleTransactionRepository.GetCompletedInRangeAsync(query.StoreId, todayStart, todayEnd, cancellationToken);
        var todayRevenue = todaySales.SelectMany(s => s.Lines).Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
        // Same empty-set fallback as GetDailySalesReportQueryHandler/GetProfitReportQueryHandler --
        // null here before the first sale of the day, which is most of every morning.
        var currency = todaySales.FirstOrDefault()?.Currency ?? "TJS";

        var stockLevels = await stockLevelRepository.GetByStoreAsync(query.StoreId, cancellationToken);
        var productsInStock = stockLevels.Count(s => s.Quantity > 0);

        return new GetStoreDashboardResult(GetStoreDashboardOutcome.Found, todaySales.Count, todayRevenue, currency, productsInStock);
    }
}

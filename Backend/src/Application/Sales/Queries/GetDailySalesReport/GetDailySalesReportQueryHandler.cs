using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetDailySalesReport;

public sealed class GetDailySalesReportQueryHandler(
    IStoreRepository storeRepository,
    ISaleTransactionRepository saleTransactionRepository) : IQueryHandler<GetDailySalesReportQuery, GetDailySalesReportResult>
{
    public async Task<GetDailySalesReportResult> Handle(GetDailySalesReportQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetDailySalesReportResult(GetDailySalesReportOutcome.StoreNotFound, null, null, null, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetDailySalesReportResult(GetDailySalesReportOutcome.Forbidden, null, null, null, null);

        var from = new DateTimeOffset(query.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(1);

        var sales = await saleTransactionRepository.GetCompletedInRangeAsync(query.StoreId, from, to, cancellationToken);

        var revenue = sales.SelectMany(s => s.Lines).Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
        var currency = sales.FirstOrDefault()?.Currency;

        return new GetDailySalesReportResult(GetDailySalesReportOutcome.Found, query.Date, sales.Count, revenue, currency);
    }
}

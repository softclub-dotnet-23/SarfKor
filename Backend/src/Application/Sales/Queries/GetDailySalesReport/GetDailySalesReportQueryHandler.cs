using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetDailySalesReport;

public sealed class GetDailySalesReportQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISaleTransactionRepository saleTransactionRepository) : IQueryHandler<GetDailySalesReportQuery, GetDailySalesReportResult>
{
    public async Task<GetDailySalesReportResult> Handle(GetDailySalesReportQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetDailySalesReportResult(GetDailySalesReportOutcome.StoreNotFound, null, null, null, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetDailySalesReportResult(GetDailySalesReportOutcome.Forbidden, null, null, null, null);

        var from = new DateTimeOffset(query.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(1);

        var sales = await saleTransactionRepository.GetCompletedInRangeAsync(query.StoreId, from, to, cancellationToken);

        var revenue = sales.SelectMany(s => s.Lines).Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
        var currency = sales.FirstOrDefault()?.Currency;

        return new GetDailySalesReportResult(GetDailySalesReportOutcome.Found, query.Date, sales.Count, revenue, currency);
    }
}

using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetProfitReport;

public sealed class GetProfitReportQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ISaleTransactionRepository saleTransactionRepository,
    ICostPriceRepository costPriceRepository) : IQueryHandler<GetProfitReportQuery, GetProfitReportResult>
{
    public async Task<GetProfitReportResult> Handle(GetProfitReportQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetProfitReportResult(GetProfitReportOutcome.StoreNotFound, null, null, null, null, null, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetProfitReportResult(GetProfitReportOutcome.Forbidden, null, null, null, null, null, null);

        var from = new DateTimeOffset(query.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(query.ToDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);

        var sales = await saleTransactionRepository.GetCompletedInRangeAsync(query.StoreId, from, to, cancellationToken);
        var costPrices = await costPriceRepository.GetLatestForStoreAsync(query.StoreId, cancellationToken);
        var costByProduct = costPrices.ToDictionary(c => c.ProductId, c => c.Amount.Amount);

        var lines = sales.SelectMany(s => s.Lines).ToList();
        var revenue = lines.Sum(l => l.UnitPriceAtSale.Amount * l.Quantity);
        var totalCost = lines.Sum(l => (costByProduct.TryGetValue(l.ProductId, out var cost) ? cost : 0) * l.Quantity);
        var currency = sales.FirstOrDefault()?.Currency;

        return new GetProfitReportResult(GetProfitReportOutcome.Found, query.FromDate, query.ToDate, revenue, totalCost, revenue - totalCost, currency);
    }
}

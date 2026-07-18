using Application.Abstractions;
using Application.Common;
using Domain.Sales;

namespace Application.Sales.Queries.GetCashierAnomalyReport;

/// <summary>
/// CLAUDE.md §10: аудит-лог фиксирует действия постфактум, но не предотвращает мошенничество
/// кассира — здесь реализована простейшая эвристика ("алерты на аномальные паттерны отмен"),
/// не полноценное фрод-обнаружение. Порог/минимальная выборка подобраны как разумная отправная
/// точка, а не калиброваны на реальных данных — при появлении таких данных их стоит пересмотреть.
/// </summary>
public sealed class GetCashierAnomalyReportQueryHandler(
    IStoreRepository storeRepository,
    ISaleTransactionRepository saleTransactionRepository) : IQueryHandler<GetCashierAnomalyReportQuery, GetCashierAnomalyReportResult>
{
    private const double VoidRateThreshold = 0.2;
    private const int MinimumSampleSize = 5;

    public async Task<GetCashierAnomalyReportResult> Handle(GetCashierAnomalyReportQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetCashierAnomalyReportResult(GetCashierAnomalyReportOutcome.StoreNotFound, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetCashierAnomalyReportResult(GetCashierAnomalyReportOutcome.Forbidden, null);

        var from = new DateTimeOffset(query.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(query.ToDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);

        var sales = await saleTransactionRepository.GetAllInRangeAsync(query.StoreId, from, to, cancellationToken);

        var cashiers = sales
            .GroupBy(s => s.CashierUserId)
            .Select(g =>
            {
                var total = g.Count();
                var voided = g.Count(s => s.Status == SaleStatus.Voided);
                var voidRate = total == 0 ? 0 : (double)voided / total;

                return new CashierAnomalyDto(
                    g.Key,
                    total,
                    voided,
                    voidRate,
                    IsAnomalous: total >= MinimumSampleSize && voidRate > VoidRateThreshold);
            })
            .OrderByDescending(c => c.VoidRate)
            .ToList();

        return new GetCashierAnomalyReportResult(GetCashierAnomalyReportOutcome.Found, cashiers);
    }
}

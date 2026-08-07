using Application.Abstractions;
using Application.Common;

namespace Application.Analytics.Queries.GetMetricsTimeSeries;

public sealed class GetMetricsTimeSeriesQueryHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IStoreRepository storeRepository) : IQueryHandler<GetMetricsTimeSeriesQuery, GetMetricsTimeSeriesResult>
{
    public async Task<GetMetricsTimeSeriesResult> Handle(GetMetricsTimeSeriesQuery query, CancellationToken cancellationToken)
    {
        var fromUtc = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var saleTimestamps = await saleTransactionRepository.GetCreatedAtAcrossPlatformInRangeAsync(fromUtc, toUtc, cancellationToken);
        var storeTimestamps = await storeRepository.GetConnectedAtInRangeAsync(fromUtc, toUtc, cancellationToken);

        var salesByDay = saleTimestamps.GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime)).ToDictionary(g => g.Key, g => g.Count());
        var storesByDay = storeTimestamps.GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime)).ToDictionary(g => g.Key, g => g.Count());

        var days = new List<MetricsDayDto>();
        for (var date = query.From; date <= query.To; date = date.AddDays(1))
            days.Add(new MetricsDayDto(date, salesByDay.GetValueOrDefault(date), storesByDay.GetValueOrDefault(date)));

        return new GetMetricsTimeSeriesResult(days);
    }
}

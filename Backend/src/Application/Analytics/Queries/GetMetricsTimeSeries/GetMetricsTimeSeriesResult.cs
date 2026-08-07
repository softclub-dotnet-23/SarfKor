namespace Application.Analytics.Queries.GetMetricsTimeSeries;

public sealed record MetricsDayDto(DateOnly Date, int Sales, int NewStores);

public sealed record GetMetricsTimeSeriesResult(IReadOnlyList<MetricsDayDto> Days);

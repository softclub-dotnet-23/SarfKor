namespace Application.Analytics.Queries.GetMetricsTimeSeries;

public sealed record GetMetricsTimeSeriesQuery(DateOnly From, DateOnly To);

namespace Application.Sales.Queries.GetProfitReport;

public enum GetProfitReportOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetProfitReportResult(
    GetProfitReportOutcome Outcome,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal? Revenue,
    decimal? TotalCost,
    decimal? Profit,
    string? Currency);

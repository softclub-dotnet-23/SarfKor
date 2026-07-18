namespace Application.Sales.Queries.GetDailySalesReport;

public enum GetDailySalesReportOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetDailySalesReportResult(
    GetDailySalesReportOutcome Outcome,
    DateOnly? Date,
    int? SalesCount,
    decimal? Revenue,
    string? Currency);

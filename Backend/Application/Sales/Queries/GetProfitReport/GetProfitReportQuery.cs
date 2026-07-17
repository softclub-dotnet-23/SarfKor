namespace Application.Sales.Queries.GetProfitReport;

public sealed record GetProfitReportQuery(int StoreId, DateOnly FromDate, DateOnly ToDate, string RequestedByUserId);

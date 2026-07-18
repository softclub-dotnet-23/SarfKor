namespace Application.Sales.Queries.GetCashierAnomalyReport;

public sealed record GetCashierAnomalyReportQuery(int StoreId, DateOnly FromDate, DateOnly ToDate, string RequestedByUserId);

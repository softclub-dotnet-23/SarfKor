namespace Application.Sales.Queries.GetDailySalesReport;

public sealed record GetDailySalesReportQuery(int StoreId, DateOnly Date, string RequestedByUserId);

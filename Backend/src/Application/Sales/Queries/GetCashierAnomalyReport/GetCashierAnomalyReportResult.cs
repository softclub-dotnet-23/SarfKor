namespace Application.Sales.Queries.GetCashierAnomalyReport;

public enum GetCashierAnomalyReportOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record CashierAnomalyDto(string CashierUserId, int TotalSales, int VoidedSales, double VoidRate, bool IsAnomalous);

public sealed record GetCashierAnomalyReportResult(GetCashierAnomalyReportOutcome Outcome, IReadOnlyList<CashierAnomalyDto>? Cashiers);

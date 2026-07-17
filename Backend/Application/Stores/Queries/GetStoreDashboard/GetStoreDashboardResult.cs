namespace Application.Stores.Queries.GetStoreDashboard;

public enum GetStoreDashboardOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetStoreDashboardResult(
    GetStoreDashboardOutcome Outcome,
    int? TodaySalesCount,
    decimal? TodayRevenue,
    string? Currency,
    int? ProductsInStockCount);

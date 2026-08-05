using Domain.Stores;

namespace Application.Analytics.Queries.GetPlatformMetrics;

public sealed record PlanSubscriberCountDto(string SubscriptionPlanName, int Count);
public sealed record ProblemStoreDto(int StoreId, string StoreName, int ReportCount);
public sealed record SilentStoreDto(int StoreId, string StoreName, DateTimeOffset LastSaleAt);
public sealed record NoSalesStoreDto(int StoreId, string StoreName, DateTimeOffset ConnectedAt);

public sealed record GetPlatformMetricsResult(
    IReadOnlyDictionary<StoreStatus, int> StoresByStatus,
    IReadOnlyList<PlanSubscriberCountDto> ActiveSubscriptionsByPlan,
    int ActiveSubscriptionsTotal,
    decimal EstimatedMonthlyRevenue,
    string RevenueCurrency,
    int TrialsEndingThisWeek,
    int PastDueCount,
    int SalesLast7Days,
    int SalesLast30Days,
    int NewPriceEntriesLast7Days,
    int ActiveConsumersLast30Days,
    IReadOnlyList<NoSalesStoreDto> StoresWithNoSales,
    IReadOnlyList<SilentStoreDto> SilentStores,
    IReadOnlyList<ProblemStoreDto> ProblemStores);

using Application.Abstractions;
using Application.Common;
using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Analytics.Queries.GetPlatformMetrics;

// Every number here is a real aggregate query, never an in-memory scan of "all rows" (ADMIN_PROMPT.md
// §2.5: "считай метрики запросами к БД с агрегацией; не тяни всё в память"). Caching (§2.5: "тяжёлые
// сводки кешируй на короткое время") is the WebApi layer's job (see AdminMetricsController), not
// this handler's — Application stays free of an IMemoryCache dependency it would otherwise need
// tests to fake for no benefit to the logic itself.
public sealed class GetPlatformMetricsQueryHandler(
    IStoreRepository storeRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    ISaleTransactionRepository saleTransactionRepository,
    IPriceEntryRepository priceEntryRepository,
    IScanRepository scanRepository,
    IReportRepository reportRepository) : IQueryHandler<GetPlatformMetricsQuery, GetPlatformMetricsResult>
{
    private const int SilentStoreDays = 30;

    public async Task<GetPlatformMetricsResult> Handle(GetPlatformMetricsQuery query, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var storesByStatus = await storeRepository.CountByStatusAsync(cancellationToken);

        var activeStores = await storeRepository.GetFilteredAsync(
            0, 10_000, new StoreFilter(StoreStatus.Active, null, null, null, null, null, false), cancellationToken);
        var activeStoreIds = activeStores.Select(s => s.Id).ToList();

        var activeSubscriptions = await storeSubscriptionRepository.GetByStoreIdsAsync(activeStoreIds, cancellationToken);
        var activePlanSubs = activeSubscriptions.Where(s => s.Status == SubscriptionStatus.Active).ToList();

        var plansById = new Dictionary<int, string>();
        foreach (var planId in activePlanSubs.Select(s => s.SubscriptionPlanId).Distinct())
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is not null) plansById[planId] = plan.Name;
        }
        var byPlan = activePlanSubs
            .GroupBy(s => plansById.GetValueOrDefault(s.SubscriptionPlanId, "—"))
            .Select(g => new PlanSubscriberCountDto(g.Key, g.Count()))
            .ToList();

        var currency = activePlanSubs.FirstOrDefault()?.PriceAtIssue.Currency ?? "TJS";
        var estimatedMonthlyRevenue = activePlanSubs.Sum(s => s.PriceAtIssue.Amount);

        var trialsEndingThisWeek = (await storeSubscriptionRepository.GetEndingBeforeAsync(now.AddDays(7), cancellationToken))
            .Count(s => s.Status == SubscriptionStatus.Trial);
        var pastDueCount = (await storeSubscriptionRepository.GetPastDueAsync(cancellationToken)).Count;

        var salesLast7Days = await saleTransactionRepository.CountAcrossPlatformInRangeAsync(now.AddDays(-7), now, cancellationToken);
        var salesLast30Days = await saleTransactionRepository.CountAcrossPlatformInRangeAsync(now.AddDays(-30), now, cancellationToken);
        var newPriceEntries7d = await priceEntryRepository.CountInRangeAsync(now.AddDays(-7), now, cancellationToken);
        var activeConsumers30d = await scanRepository.CountDistinctUsersInRangeAsync(now.AddDays(-30), now, cancellationToken);

        var lastSaleByStore = await saleTransactionRepository.GetLastSaleAtByStoreIdsAsync(activeStoreIds, cancellationToken);
        var noSales = activeStores
            .Where(s => !lastSaleByStore.ContainsKey(s.Id))
            .Select(s => new NoSalesStoreDto(s.Id, s.Name, s.ConnectedAt))
            .ToList();
        var silentCutoff = now.AddDays(-SilentStoreDays);
        var silent = lastSaleByStore
            .Where(kv => kv.Value < silentCutoff)
            .Select(kv => new SilentStoreDto(kv.Key, activeStores.FirstOrDefault(s => s.Id == kv.Key)?.Name ?? $"Store #{kv.Key}", kv.Value))
            .ToList();

        var mostReported = await reportRepository.GetMostReportedStoresSinceAsync(now.AddDays(-30), 10, cancellationToken);
        var reportedStoreIds = mostReported.Select(r => r.StoreId).ToList();
        var reportedStores = await storeRepository.GetByIdsAsync(reportedStoreIds, cancellationToken);
        var problemStores = mostReported
            .Select(r => new ProblemStoreDto(r.StoreId, reportedStores.FirstOrDefault(s => s.Id == r.StoreId)?.Name ?? $"Store #{r.StoreId}", r.ReportCount))
            .ToList();

        return new GetPlatformMetricsResult(
            storesByStatus,
            byPlan,
            activePlanSubs.Count,
            estimatedMonthlyRevenue,
            currency,
            trialsEndingThisWeek,
            pastDueCount,
            salesLast7Days,
            salesLast30Days,
            newPriceEntries7d,
            activeConsumers30d,
            noSales,
            silent,
            problemStores);
    }
}

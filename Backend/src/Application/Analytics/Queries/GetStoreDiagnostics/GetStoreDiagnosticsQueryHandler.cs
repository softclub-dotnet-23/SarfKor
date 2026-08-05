using Application.Abstractions;
using Application.Common;
using Domain.Security;

namespace Application.Analytics.Queries.GetStoreDiagnostics;

public sealed class GetStoreDiagnosticsQueryHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStockLevelRepository stockLevelRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    ISaleTransactionRepository saleTransactionRepository,
    ISecurityEventRepository securityEventRepository) : IQueryHandler<GetStoreDiagnosticsQuery, GetStoreDiagnosticsResult>
{
    public async Task<GetStoreDiagnosticsResult> Handle(GetStoreDiagnosticsQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreDiagnosticsResult(GetStoreDiagnosticsOutcome.NotFound, query.StoreId,
                null, null, null, null, null, null, null, null, null, null);

        var ownerEvents = await securityEventRepository.GetByUserIdAsync(store.OwnerUserId, cancellationToken);
        var lastLogin = ownerEvents.Where(e => e.Type == SecurityEventType.LoginSucceeded).MaxBy(e => e.OccurredAt)?.OccurredAt;

        var lastSaleAt = await saleTransactionRepository.GetLastSaleAtAsync(store.Id, cancellationToken);

        var ownedLocations = await storeRepository.GetOwnedByUserIdAsync(store.OwnerUserId, cancellationToken);
        var employees = await storeEmployeeRepository.GetByStoreIdAsync(store.Id, cancellationToken);
        var stockLevels = await stockLevelRepository.GetByStoreAsync(store.Id, cancellationToken);

        var subscription = await storeSubscriptionRepository.GetByStoreIdAsync(store.Id, cancellationToken);
        string? planName = null;
        if (subscription is not null)
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(subscription.SubscriptionPlanId, cancellationToken);
            planName = plan?.Name;
        }

        return new GetStoreDiagnosticsResult(
            GetStoreDiagnosticsOutcome.Found,
            store.Id,
            store.Status,
            lastLogin,
            lastSaleAt,
            ownedLocations.Count,
            employees.Count,
            stockLevels.Select(s => s.ProductId).Distinct().Count(),
            stockLevels.Sum(s => s.Quantity),
            subscription?.Status,
            planName,
            subscription?.CurrentPeriodEndsAt);
    }
}

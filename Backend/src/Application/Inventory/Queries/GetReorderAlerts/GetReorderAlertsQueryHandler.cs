using Application.Abstractions;
using Application.Common;

namespace Application.Inventory.Queries.GetReorderAlerts;

public sealed class GetReorderAlertsQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IReorderRuleRepository reorderRuleRepository,
    IStockLevelRepository stockLevelRepository) : IQueryHandler<GetReorderAlertsQuery, GetReorderAlertsResult>
{
    public async Task<GetReorderAlertsResult> Handle(GetReorderAlertsQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetReorderAlertsResult(GetReorderAlertsOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetReorderAlertsResult(GetReorderAlertsOutcome.Forbidden, null);

        var rules = await reorderRuleRepository.GetActiveByStoreIdAsync(query.StoreId, cancellationToken);
        var stockLevels = await stockLevelRepository.GetByStoreAsync(query.StoreId, cancellationToken);
        var quantityByProduct = stockLevels.ToDictionary(s => s.ProductId, s => s.Quantity);

        var alerts = rules
            .Select(r => new { Rule = r, CurrentQuantity = quantityByProduct.GetValueOrDefault(r.ProductId, 0) })
            .Where(x => x.CurrentQuantity <= x.Rule.ThresholdQuantity)
            .Select(x => new ReorderAlertDto(x.Rule.ProductId, x.CurrentQuantity, x.Rule.ThresholdQuantity, x.Rule.ReorderQuantity, x.Rule.PreferredSupplierId))
            .ToList();

        return new GetReorderAlertsResult(GetReorderAlertsOutcome.Found, alerts);
    }
}

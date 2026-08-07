using Application.Abstractions;
using Application.Common;

namespace Application.Stores.Queries.GetStoreDetail;

public sealed class GetStoreDetailQueryHandler(
    IStoreRepository storeRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IAuthService authService) : IQueryHandler<GetStoreDetailQuery, GetStoreDetailResult>
{
    public async Task<GetStoreDetailResult> Handle(GetStoreDetailQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreDetailResult(GetStoreDetailOutcome.NotFound, query.StoreId,
                null, null, null, null, null, null, null, null, null, null, null, null);

        var emails = await authService.GetEmailsByUserIdsAsync([store.OwnerUserId], cancellationToken);
        var subscription = await storeSubscriptionRepository.GetByStoreIdAsync(store.Id, cancellationToken);

        AdminStoreSubscriptionDto? subDto = null;
        if (subscription is not null)
        {
            var plan = await subscriptionPlanRepository.GetByIdAsync(subscription.SubscriptionPlanId, cancellationToken);
            subDto = new AdminStoreSubscriptionDto(
                subscription.Id,
                subscription.SubscriptionPlanId,
                plan?.Name ?? "—",
                subscription.Status,
                subscription.StartedAt,
                subscription.CurrentPeriodEndsAt,
                subscription.PriceAtIssue.Amount,
                subscription.PriceAtIssue.Currency,
                subscription.Note);
        }

        return new GetStoreDetailResult(
            GetStoreDetailOutcome.Found,
            store.Id,
            store.Name,
            store.Address,
            store.Location.Latitude,
            store.Location.Longitude,
            store.Status,
            store.StatusReason,
            store.StatusChangedAt,
            store.OwnerUserId,
            emails.GetValueOrDefault(store.OwnerUserId),
            store.IsVatPayer,
            store.TaxRegime,
            subDto);
    }
}

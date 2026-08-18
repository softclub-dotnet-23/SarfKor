using Application.Abstractions;
using Application.Common;

namespace Application.Subscriptions.Queries.GetMyStoreSubscriptionStatus;

public sealed class GetMyStoreSubscriptionStatusQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreSubscriptionRepository storeSubscriptionRepository) : IQueryHandler<GetMyStoreSubscriptionStatusQuery, GetMyStoreSubscriptionStatusResult>
{
    public async Task<GetMyStoreSubscriptionStatusResult> Handle(GetMyStoreSubscriptionStatusQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetMyStoreSubscriptionStatusResult(GetMyStoreSubscriptionStatusOutcome.StoreNotFound, null, null, null, false);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetMyStoreSubscriptionStatusResult(GetMyStoreSubscriptionStatusOutcome.Forbidden, null, null, null, false);

        var isOwner = await storeAccessAuthorizer.IsOwnerAsync(query.StoreId, query.RequestedByUserId, cancellationToken);
        var subscription = await storeSubscriptionRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        // Same authorizer every write handler gates on -- see GetMyStoreSubscriptionStatusResult's
        // doc comment for why this can't be re-derived from Status on the frontend instead.
        var isOperational = await storeAccessAuthorizer.IsOperationalAsync(query.StoreId, cancellationToken);

        // No StoreSubscription row at all (e.g. a store created before one was ever issued) reads
        // the same as "not current" to the caller -- there's nothing to show a date for either way.
        return new GetMyStoreSubscriptionStatusResult(
            GetMyStoreSubscriptionStatusOutcome.Found,
            subscription?.Status,
            subscription?.CurrentPeriodEndsAt,
            isOwner,
            isOperational);
    }
}

using Application.Abstractions;
using Application.Common;

namespace Application.Payments.Queries.GetStoreCreditBalance;

public sealed class GetStoreCreditBalanceQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreCreditRepository storeCreditRepository) : IQueryHandler<GetStoreCreditBalanceQuery, GetStoreCreditBalanceResult>
{
    public async Task<GetStoreCreditBalanceResult> Handle(GetStoreCreditBalanceQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome.StoreNotFound, null, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome.Forbidden, null, null);

        var credit = await storeCreditRepository.GetByStoreAndCustomerAsync(query.StoreId, query.CustomerId, cancellationToken);
        return new GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome.Found, credit?.Balance.Amount ?? 0, credit?.Balance.Currency);
    }
}

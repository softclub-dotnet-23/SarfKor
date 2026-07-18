using Application.Abstractions;
using Application.Common;

namespace Application.Payments.Queries.GetStoreCreditBalance;

public sealed class GetStoreCreditBalanceQueryHandler(
    IStoreRepository storeRepository,
    IStoreCreditRepository storeCreditRepository) : IQueryHandler<GetStoreCreditBalanceQuery, GetStoreCreditBalanceResult>
{
    public async Task<GetStoreCreditBalanceResult> Handle(GetStoreCreditBalanceQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome.StoreNotFound, null, null);

        if (store.OwnerUserId != query.RequestedByUserId)
            return new GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome.Forbidden, null, null);

        var credit = await storeCreditRepository.GetByStoreAndCustomerAsync(query.StoreId, query.CustomerId, cancellationToken);
        return new GetStoreCreditBalanceResult(GetStoreCreditBalanceOutcome.Found, credit?.Balance.Amount ?? 0, credit?.Balance.Currency);
    }
}

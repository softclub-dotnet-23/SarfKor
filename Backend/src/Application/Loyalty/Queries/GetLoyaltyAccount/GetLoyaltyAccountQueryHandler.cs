using Application.Abstractions;
using Application.Common;

namespace Application.Loyalty.Queries.GetLoyaltyAccount;

public sealed class GetLoyaltyAccountQueryHandler(
    ILoyaltyAccountRepository loyaltyAccountRepository,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer) : IQueryHandler<GetLoyaltyAccountQuery, GetLoyaltyAccountResult>
{
    public async Task<GetLoyaltyAccountResult> Handle(GetLoyaltyAccountQuery query, CancellationToken cancellationToken)
    {
        var program = await loyaltyProgramRepository.GetByIdAsync(query.LoyaltyProgramId, cancellationToken);
        if (program is null)
            return new GetLoyaltyAccountResult(GetLoyaltyAccountOutcome.NotFound, null, null);

        // Without this, any authenticated StorePartner could read any other store's customer
        // loyalty balance just by guessing/incrementing customerId+loyaltyProgramId (IDOR).
        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(program.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetLoyaltyAccountResult(GetLoyaltyAccountOutcome.Forbidden, null, null);

        var account = await loyaltyAccountRepository.GetByCustomerAndProgramAsync(query.CustomerId, query.LoyaltyProgramId, cancellationToken);
        return account is null
            ? new GetLoyaltyAccountResult(GetLoyaltyAccountOutcome.NotFound, null, null)
            : new GetLoyaltyAccountResult(GetLoyaltyAccountOutcome.Found, account.Id, account.PointsBalance);
    }
}

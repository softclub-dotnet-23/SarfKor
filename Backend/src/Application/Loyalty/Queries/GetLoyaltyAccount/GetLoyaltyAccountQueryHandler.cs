using Application.Abstractions;
using Application.Common;

namespace Application.Loyalty.Queries.GetLoyaltyAccount;

public sealed class GetLoyaltyAccountQueryHandler(ILoyaltyAccountRepository loyaltyAccountRepository) : IQueryHandler<GetLoyaltyAccountQuery, GetLoyaltyAccountResult>
{
    public async Task<GetLoyaltyAccountResult> Handle(GetLoyaltyAccountQuery query, CancellationToken cancellationToken)
    {
        var account = await loyaltyAccountRepository.GetByCustomerAndProgramAsync(query.CustomerId, query.LoyaltyProgramId, cancellationToken);
        return account is null
            ? new GetLoyaltyAccountResult(null, null)
            : new GetLoyaltyAccountResult(account.Id, account.PointsBalance);
    }
}

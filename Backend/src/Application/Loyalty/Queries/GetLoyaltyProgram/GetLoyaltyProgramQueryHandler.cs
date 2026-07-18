using Application.Abstractions;
using Application.Common;

namespace Application.Loyalty.Queries.GetLoyaltyProgram;

public sealed class GetLoyaltyProgramQueryHandler(ILoyaltyProgramRepository loyaltyProgramRepository) : IQueryHandler<GetLoyaltyProgramQuery, GetLoyaltyProgramResult>
{
    public async Task<GetLoyaltyProgramResult> Handle(GetLoyaltyProgramQuery query, CancellationToken cancellationToken)
    {
        var program = await loyaltyProgramRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        return program is null
            ? new GetLoyaltyProgramResult(null, null, null, null)
            : new GetLoyaltyProgramResult(program.Id, program.PointsPerCurrencyUnit, program.RedemptionRate, program.IsActive);
    }
}

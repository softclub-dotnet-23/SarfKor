using Application.Abstractions;
using Application.Common;
using Domain.Loyalty;

namespace Application.Loyalty.Commands.EarnLoyaltyPoints;

public sealed class EarnLoyaltyPointsCommandHandler(
    ILoyaltyAccountRepository loyaltyAccountRepository,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    ILoyaltyTransactionRepository loyaltyTransactionRepository,
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<EarnLoyaltyPointsCommand, EarnLoyaltyPointsResult>
{
    public async Task<EarnLoyaltyPointsResult> Handle(EarnLoyaltyPointsCommand command, CancellationToken cancellationToken)
    {
        var account = await loyaltyAccountRepository.GetByIdAsync(command.LoyaltyAccountId, cancellationToken);
        if (account is null)
            return new EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome.AccountNotFound, null);

        var program = await loyaltyProgramRepository.GetByIdAsync(account.LoyaltyProgramId, cancellationToken);
        var store = program is null ? null : await storeRepository.GetByIdAsync(program.StoreId, cancellationToken);
        if (store is null)
            return new EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome.Forbidden, null);

        if (store.OwnerUserId != command.PerformedByUserId
            && !await storeEmployeeRepository.IsEmployeeAsync(store.Id, command.PerformedByUserId, cancellationToken))
            return new EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome.Forbidden, null);

        account.PointsBalance += command.Points;
        loyaltyTransactionRepository.Add(new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            SaleTransactionId = command.SaleTransactionId,
            PointsDelta = command.Points,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome.Earned, account.PointsBalance);
    }
}

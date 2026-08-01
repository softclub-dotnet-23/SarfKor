using Application.Abstractions;
using Application.Common;
using Domain.Loyalty;

namespace Application.Loyalty.Commands.EarnLoyaltyPoints;

public sealed class EarnLoyaltyPointsCommandHandler(
    ILoyaltyAccountRepository loyaltyAccountRepository,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    ILoyaltyTransactionRepository loyaltyTransactionRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<EarnLoyaltyPointsCommand, EarnLoyaltyPointsResult>
{
    public async Task<EarnLoyaltyPointsResult> Handle(EarnLoyaltyPointsCommand command, CancellationToken cancellationToken)
    {
        var account = await loyaltyAccountRepository.GetByIdAsync(command.LoyaltyAccountId, cancellationToken);
        if (account is null)
            return new EarnLoyaltyPointsResult(EarnLoyaltyPointsOutcome.AccountNotFound, null);

        var program = await loyaltyProgramRepository.GetByIdAsync(account.LoyaltyProgramId, cancellationToken);
        if (program is null || !await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(program.StoreId, command.PerformedByUserId, cancellationToken))
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

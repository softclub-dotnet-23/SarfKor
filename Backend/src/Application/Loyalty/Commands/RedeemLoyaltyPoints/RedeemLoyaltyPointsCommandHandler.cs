using Application.Abstractions;
using Application.Common;
using Domain.Loyalty;

namespace Application.Loyalty.Commands.RedeemLoyaltyPoints;

public sealed class RedeemLoyaltyPointsCommandHandler(
    ILoyaltyAccountRepository loyaltyAccountRepository,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    ILoyaltyTransactionRepository loyaltyTransactionRepository,
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RedeemLoyaltyPointsCommand, RedeemLoyaltyPointsResult>
{
    public async Task<RedeemLoyaltyPointsResult> Handle(RedeemLoyaltyPointsCommand command, CancellationToken cancellationToken)
    {
        var account = await loyaltyAccountRepository.GetByIdAsync(command.LoyaltyAccountId, cancellationToken);
        if (account is null)
            return new RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome.AccountNotFound, null);

        var program = await loyaltyProgramRepository.GetByIdAsync(account.LoyaltyProgramId, cancellationToken);
        var store = program is null ? null : await storeRepository.GetByIdAsync(program.StoreId, cancellationToken);
        if (store is null)
            return new RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome.Forbidden, null);

        if (store.OwnerUserId != command.PerformedByUserId
            && !await storeEmployeeRepository.IsEmployeeAsync(store.Id, command.PerformedByUserId, cancellationToken))
            return new RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome.Forbidden, null);

        if (account.PointsBalance < command.Points)
            return new RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome.InsufficientPoints, account.PointsBalance);

        account.PointsBalance -= command.Points;
        loyaltyTransactionRepository.Add(new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            SaleTransactionId = null,
            PointsDelta = -command.Points,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RedeemLoyaltyPointsResult(RedeemLoyaltyPointsOutcome.Redeemed, account.PointsBalance);
    }
}

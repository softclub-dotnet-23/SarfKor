using Application.Abstractions;
using Application.Common;

namespace Application.Payments.Commands.RedeemStoreCredit;

public sealed class RedeemStoreCreditCommandHandler(
    IStoreRepository storeRepository,
    IStoreEmployeeRepository storeEmployeeRepository,
    IStoreCreditRepository storeCreditRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RedeemStoreCreditCommand, RedeemStoreCreditResult>
{
    public async Task<RedeemStoreCreditResult> Handle(RedeemStoreCreditCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId
            && !await storeEmployeeRepository.IsEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.Forbidden, null);

        var credit = await storeCreditRepository.GetByStoreAndCustomerAsync(command.StoreId, command.CustomerId, cancellationToken);
        if (credit is null)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.NoCreditOnFile, null);

        if (credit.Balance.Amount < command.Amount)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.InsufficientBalance, credit.Balance.Amount);

        credit.Balance = credit.Balance with { Amount = credit.Balance.Amount - command.Amount };
        credit.UpdatedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.Redeemed, credit.Balance.Amount);
    }
}

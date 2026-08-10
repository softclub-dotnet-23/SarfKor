using Application.Abstractions;
using Application.Common;

namespace Application.Payments.Commands.RedeemStoreCredit;

public sealed class RedeemStoreCreditCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IStoreCreditRepository storeCreditRepository) : ICommandHandler<RedeemStoreCreditCommand, RedeemStoreCreditResult>
{
    public async Task<RedeemStoreCreditResult> Handle(RedeemStoreCreditCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.SubscriptionInactive, null);

        var credit = await storeCreditRepository.GetByStoreAndCustomerAsync(command.StoreId, command.CustomerId, cancellationToken);
        if (credit is null)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.NoCreditOnFile, null);

        if (credit.Balance.Currency != command.Currency)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.CurrencyMismatch, credit.Balance.Amount);

        if (credit.Balance.Amount < command.Amount)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.InsufficientBalance, credit.Balance.Amount);

        // Atomic, race-safe debit — see RedeemGiftCardCommandHandler for the same pattern and why
        // the balance check above is only a fast-path pre-check, not the actual guard.
        var debited = await storeCreditRepository.TryDebitAsync(credit.Id, command.Amount, cancellationToken);
        if (!debited)
            return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.InsufficientBalance, credit.Balance.Amount);

        return new RedeemStoreCreditResult(RedeemStoreCreditOutcome.Redeemed, credit.Balance.Amount - command.Amount);
    }
}

using Application.Abstractions;
using Application.Common;
using Domain.Payments;

namespace Application.Payments.Commands.RedeemGiftCard;

public sealed class RedeemGiftCardCommandHandler(
    IGiftCardRepository giftCardRepository,
    IGiftCardRedemptionRepository giftCardRedemptionRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<RedeemGiftCardCommand, RedeemGiftCardResult>
{
    public async Task<RedeemGiftCardResult> Handle(RedeemGiftCardCommand command, CancellationToken cancellationToken)
    {
        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.SubscriptionInactive, null);

        var giftCard = await giftCardRepository.GetByCodeAsync(command.Code, cancellationToken);
        if (giftCard is null)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.NotFound, null);

        if (!giftCard.IsActive)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.Inactive, giftCard.Balance.Amount);

        if (giftCard.ExpiresAt is not null && giftCard.ExpiresAt < DateTimeOffset.UtcNow)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.Expired, giftCard.Balance.Amount);

        if (giftCard.Balance.Currency != command.Currency)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.CurrencyMismatch, giftCard.Balance.Amount);

        if (giftCard.Balance.Amount < command.Amount)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.InsufficientBalance, giftCard.Balance.Amount);

        // Atomic, race-safe debit — the balance check above is only a fast-path pre-check; the
        // ExecuteUpdateAsync's own WHERE guard is what actually prevents a concurrent redemption
        // from over-spending the card (see StockLevelRepository.TryDecrementAsync for the pattern).
        var debited = await giftCardRepository.TryDebitAsync(giftCard.Id, command.Amount, cancellationToken);
        if (!debited)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.InsufficientBalance, giftCard.Balance.Amount);

        giftCardRedemptionRepository.Add(new GiftCardRedemption
        {
            GiftCardId = giftCard.Id,
            StoreId = command.StoreId,
            Amount = command.Amount,
            SaleTransactionId = null,
            RedeemedAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RedeemGiftCardResult(RedeemGiftCardOutcome.Redeemed, giftCard.Balance.Amount - command.Amount);
    }
}

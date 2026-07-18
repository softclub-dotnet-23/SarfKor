using Application.Abstractions;
using Application.Common;
using Domain.ValueObjects;

namespace Application.Payments.Commands.RedeemGiftCard;

public sealed class RedeemGiftCardCommandHandler(
    IGiftCardRepository giftCardRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RedeemGiftCardCommand, RedeemGiftCardResult>
{
    public async Task<RedeemGiftCardResult> Handle(RedeemGiftCardCommand command, CancellationToken cancellationToken)
    {
        var giftCard = await giftCardRepository.GetByCodeAsync(command.Code, cancellationToken);
        if (giftCard is null)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.NotFound, null);

        if (!giftCard.IsActive)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.Inactive, giftCard.Balance.Amount);

        if (giftCard.ExpiresAt is not null && giftCard.ExpiresAt < DateTimeOffset.UtcNow)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.Expired, giftCard.Balance.Amount);

        if (giftCard.Balance.Amount < command.Amount)
            return new RedeemGiftCardResult(RedeemGiftCardOutcome.InsufficientBalance, giftCard.Balance.Amount);

        giftCard.Balance = giftCard.Balance with { Amount = giftCard.Balance.Amount - command.Amount };
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RedeemGiftCardResult(RedeemGiftCardOutcome.Redeemed, giftCard.Balance.Amount);
    }
}

using Application.Abstractions;
using Application.Common;
using Domain.Payments;
using Domain.ValueObjects;

namespace Application.Payments.Commands.IssueGiftCard;

public sealed class IssueGiftCardCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IGiftCardRepository giftCardRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<IssueGiftCardCommand, IssueGiftCardResult>
{
    public async Task<IssueGiftCardResult> Handle(IssueGiftCardCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new IssueGiftCardResult(IssueGiftCardOutcome.StoreNotFound, null, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new IssueGiftCardResult(IssueGiftCardOutcome.Forbidden, null, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new IssueGiftCardResult(IssueGiftCardOutcome.SubscriptionInactive, null, null);

        var giftCard = new GiftCard
        {
            Code = GenerateCode(),
            Balance = new Money(command.Amount, command.Currency),
            IsActive = true,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = command.ExpiresAt,
            IssuingStoreId = command.StoreId,
            IssuedByUserId = command.PerformedByUserId
        };

        giftCardRepository.Add(giftCard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueGiftCardResult(IssueGiftCardOutcome.Issued, giftCard.Id, giftCard.Code);
    }

    private static string GenerateCode() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
}

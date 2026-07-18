using Application.Abstractions;
using Application.Common;
using Domain.Payments;
using Domain.ValueObjects;

namespace Application.Payments.Commands.IssueGiftCard;

public sealed class IssueGiftCardCommandHandler(
    IGiftCardRepository giftCardRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<IssueGiftCardCommand, IssueGiftCardResult>
{
    public async Task<IssueGiftCardResult> Handle(IssueGiftCardCommand command, CancellationToken cancellationToken)
    {
        var giftCard = new GiftCard
        {
            Code = GenerateCode(),
            Balance = new Money(command.Amount, command.Currency),
            IsActive = true,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = command.ExpiresAt
        };

        giftCardRepository.Add(giftCard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueGiftCardResult(giftCard.Id, giftCard.Code);
    }

    private static string GenerateCode() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
}

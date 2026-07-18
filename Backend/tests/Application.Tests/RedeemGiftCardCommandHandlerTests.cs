using Application.Abstractions;
using Application.Payments.Commands.RedeemGiftCard;
using Domain.Payments;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RedeemGiftCardCommandHandlerTests
{
    private const string Code = "ABC123";

    private readonly Mock<IGiftCardRepository> _giftCardRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RedeemGiftCardCommandHandler CreateHandler() => new(_giftCardRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_CodeNotFound_ReturnsNotFound()
    {
        _giftCardRepository.Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>())).ReturnsAsync((GiftCard?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemGiftCardCommand(Code, 10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_InactiveCard_ReturnsInactive()
    {
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard { Code = Code, Balance = new Money(50, "TJS"), IsActive = false, IssuedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemGiftCardCommand(Code, 10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.Inactive, result.Outcome);
    }

    [Fact]
    public async Task Handle_ExpiredCard_ReturnsExpired()
    {
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard
            {
                Code = Code,
                Balance = new Money(50, "TJS"),
                IsActive = true,
                IssuedAt = DateTimeOffset.UtcNow.AddDays(-100),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemGiftCardCommand(Code, 10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.Expired, result.Outcome);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ReturnsInsufficientBalance()
    {
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard { Code = Code, Balance = new Money(5, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemGiftCardCommand(Code, 10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.InsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidRedemption_DecrementsBalance()
    {
        var giftCard = new GiftCard { Code = Code, Balance = new Money(50, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemGiftCardCommand(Code, 20), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.Redeemed, result.Outcome);
        Assert.Equal(30, result.RemainingBalance);
        Assert.Equal(30, giftCard.Balance.Amount);
    }
}

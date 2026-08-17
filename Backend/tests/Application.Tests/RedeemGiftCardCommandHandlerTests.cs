using Application.Abstractions;
using Application.Payments.Commands.RedeemGiftCard;
using Domain.Payments;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RedeemGiftCardCommandHandlerTests
{
    private const string Code = "ABC123";
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IGiftCardRepository> _giftCardRepository = new();
    private readonly Mock<IGiftCardRedemptionRepository> _giftCardRedemptionRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RedeemGiftCardCommandHandlerTests()
    {
        _storeAccessAuthorizer
            .Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private RedeemGiftCardCommandHandler CreateHandler() =>
        new(_giftCardRepository.Object, _giftCardRedemptionRepository.Object, _storeAccessAuthorizer.Object, _unitOfWork.Object);

    private static RedeemGiftCardCommand ValidCommand(decimal amount) => new(Code, amount, "TJS", StoreId, OwnerId);

    [Fact]
    public async Task Handle_NotOwnerOrEmployee_ReturnsForbidden()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(10) with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_CodeNotFound_ReturnsNotFound()
    {
        _giftCardRepository.Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>())).ReturnsAsync((GiftCard?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_InactiveCard_ReturnsInactive()
    {
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard { Code = Code, Balance = new Money(50, "TJS"), IsActive = false, IssuedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(10), CancellationToken.None);

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
        var result = await handler.Handle(ValidCommand(10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.Expired, result.Outcome);
    }

    [Fact]
    public async Task Handle_CurrencyMismatch_ReturnsCurrencyMismatch()
    {
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard { Code = Code, Balance = new Money(50, "USD"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.CurrencyMismatch, result.Outcome);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ReturnsInsufficientBalance()
    {
        _giftCardRepository
            .Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GiftCard { Code = Code, Balance = new Money(5, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(10), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.InsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_ConcurrentRedemptionLosesRace_ReturnsInsufficientBalance()
    {
        // The pre-check passes (balance looks sufficient), but the atomic debit itself fails —
        // a concurrent redemption spent the card in between. This is exactly the race
        // TryDebitAsync exists to catch.
        var giftCard = new GiftCard { Id = 1, Code = Code, Balance = new Money(50, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);
        _giftCardRepository.Setup(r => r.TryDebitAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(20), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.InsufficientBalance, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidRedemption_DebitsAtomicallyAndRecordsRedemption()
    {
        var giftCard = new GiftCard { Id = 1, Code = Code, Balance = new Money(50, "TJS"), IsActive = true, IssuedAt = DateTimeOffset.UtcNow };
        _giftCardRepository.Setup(r => r.GetByCodeAsync(Code, It.IsAny<CancellationToken>())).ReturnsAsync(giftCard);
        _giftCardRepository.Setup(r => r.TryDebitAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(20), CancellationToken.None);

        Assert.Equal(RedeemGiftCardOutcome.Redeemed, result.Outcome);
        Assert.Equal(30, result.RemainingBalance);
        _giftCardRepository.Verify(r => r.TryDebitAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
        _giftCardRedemptionRepository.Verify(
            r => r.Add(It.Is<GiftCardRedemption>(g => g.GiftCardId == 1 && g.StoreId == StoreId && g.Amount == 20)),
            Times.Once);
    }
}

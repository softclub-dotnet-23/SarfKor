using Application.Abstractions;
using Application.Loyalty.Commands.RedeemLoyaltyPoints;
using Domain.Loyalty;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RedeemLoyaltyPointsCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int AccountId = 1;
    private const int ProgramId = 1;
    private const int StoreId = 1;

    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<ILoyaltyProgramRepository> _loyaltyProgramRepository = new();
    private readonly Mock<ILoyaltyTransactionRepository> _loyaltyTransactionRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RedeemLoyaltyPointsCommandHandler CreateHandler() => new(
        _loyaltyAccountRepository.Object,
        _loyaltyProgramRepository.Object,
        _loyaltyTransactionRepository.Object,
        _storeRepository.Object,
        _unitOfWork.Object);

    private void SetupAccountAndOwnership(int balance)
    {
        _loyaltyAccountRepository
            .Setup(r => r.GetByIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyAccount { CustomerId = 1, LoyaltyProgramId = ProgramId, PointsBalance = balance });
        _loyaltyProgramRepository
            .Setup(r => r.GetByIdAsync(ProgramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyProgram { StoreId = StoreId, PointsPerCurrencyUnit = 1, RedemptionRate = 0.1m, IsActive = true });
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsAccountNotFound()
    {
        _loyaltyAccountRepository.Setup(r => r.GetByIdAsync(AccountId, It.IsAny<CancellationToken>())).ReturnsAsync((LoyaltyAccount?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemLoyaltyPointsCommand(AccountId, 10, OwnerId), CancellationToken.None);

        Assert.Equal(RedeemLoyaltyPointsOutcome.AccountNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_InsufficientPoints_ReturnsInsufficientPointsWithoutMutating()
    {
        SetupAccountAndOwnership(balance: 5);

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemLoyaltyPointsCommand(AccountId, 10, OwnerId), CancellationToken.None);

        Assert.Equal(RedeemLoyaltyPointsOutcome.InsufficientPoints, result.Outcome);
        Assert.Equal(5, result.NewBalance);
        _loyaltyTransactionRepository.Verify(r => r.Add(It.IsAny<LoyaltyTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SufficientPoints_DecrementsBalance()
    {
        SetupAccountAndOwnership(balance: 20);

        var handler = CreateHandler();
        var result = await handler.Handle(new RedeemLoyaltyPointsCommand(AccountId, 10, OwnerId), CancellationToken.None);

        Assert.Equal(RedeemLoyaltyPointsOutcome.Redeemed, result.Outcome);
        Assert.Equal(10, result.NewBalance);
        _loyaltyTransactionRepository.Verify(r => r.Add(It.Is<LoyaltyTransaction>(t => t.PointsDelta == -10)), Times.Once);
    }
}

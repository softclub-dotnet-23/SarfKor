using Application.Abstractions;
using Application.Loyalty.Commands.EarnLoyaltyPoints;
using Domain.Loyalty;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class EarnLoyaltyPointsCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int AccountId = 1;
    private const int ProgramId = 1;
    private const int StoreId = 1;

    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<ILoyaltyProgramRepository> _loyaltyProgramRepository = new();
    private readonly Mock<ILoyaltyTransactionRepository> _loyaltyTransactionRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public EarnLoyaltyPointsCommandHandlerTests() =>
        _storeEmployeeRepository
            .Setup(r => r.IsEmployeeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

    private EarnLoyaltyPointsCommandHandler CreateHandler() => new(
        _loyaltyAccountRepository.Object,
        _loyaltyProgramRepository.Object,
        _loyaltyTransactionRepository.Object,
        _storeRepository.Object,
        _storeEmployeeRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsAccountNotFound()
    {
        _loyaltyAccountRepository.Setup(r => r.GetByIdAsync(AccountId, It.IsAny<CancellationToken>())).ReturnsAsync((LoyaltyAccount?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new EarnLoyaltyPointsCommand(AccountId, 10, null, OwnerId), CancellationToken.None);

        Assert.Equal(EarnLoyaltyPointsOutcome.AccountNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotStoreOwner_ReturnsForbidden()
    {
        _loyaltyAccountRepository
            .Setup(r => r.GetByIdAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyAccount { CustomerId = 1, LoyaltyProgramId = ProgramId, PointsBalance = 5 });
        _loyaltyProgramRepository
            .Setup(r => r.GetByIdAsync(ProgramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyProgram { StoreId = StoreId, PointsPerCurrencyUnit = 1, RedemptionRate = 0.1m, IsActive = true });
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new EarnLoyaltyPointsCommand(AccountId, 10, null, "someone-else"), CancellationToken.None);

        Assert.Equal(EarnLoyaltyPointsOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_IncrementsBalanceAndRecordsTransaction()
    {
        var account = new LoyaltyAccount { CustomerId = 1, LoyaltyProgramId = ProgramId, PointsBalance = 5 };
        _loyaltyAccountRepository.Setup(r => r.GetByIdAsync(AccountId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _loyaltyProgramRepository
            .Setup(r => r.GetByIdAsync(ProgramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyProgram { StoreId = StoreId, PointsPerCurrencyUnit = 1, RedemptionRate = 0.1m, IsActive = true });
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new EarnLoyaltyPointsCommand(AccountId, 10, 42, OwnerId), CancellationToken.None);

        Assert.Equal(EarnLoyaltyPointsOutcome.Earned, result.Outcome);
        Assert.Equal(15, result.NewBalance);
        Assert.Equal(15, account.PointsBalance);
        _loyaltyTransactionRepository.Verify(r => r.Add(It.Is<LoyaltyTransaction>(t => t.PointsDelta == 10 && t.SaleTransactionId == 42)), Times.Once);
    }
}

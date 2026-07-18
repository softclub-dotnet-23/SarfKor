using Application.Abstractions;
using Application.Loyalty.Commands.EnrollCustomerInLoyalty;
using Domain.Customers;
using Domain.Loyalty;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class EnrollCustomerInLoyaltyCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int CustomerId = 1;
    private const int ProgramId = 1;
    private const int StoreId = 1;

    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ILoyaltyProgramRepository> _loyaltyProgramRepository = new();
    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private EnrollCustomerInLoyaltyCommandHandler CreateHandler() => new(
        _customerRepository.Object,
        _loyaltyProgramRepository.Object,
        _loyaltyAccountRepository.Object,
        _storeRepository.Object,
        _unitOfWork.Object);

    private void SetupValidCustomerAndProgram()
    {
        _customerRepository
            .Setup(r => r.GetByIdAsync(CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { PhoneNumber = "+992900000000", CreatedAt = DateTimeOffset.UtcNow });
        _loyaltyProgramRepository
            .Setup(r => r.GetByIdAsync(ProgramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyProgram { StoreId = StoreId, PointsPerCurrencyUnit = 1, RedemptionRate = 0.1m, IsActive = true });
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ReturnsCustomerNotFound()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new EnrollCustomerInLoyaltyCommand(CustomerId, ProgramId, OwnerId), CancellationToken.None);

        Assert.Equal(EnrollCustomerInLoyaltyOutcome.CustomerNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotStoreOwner_ReturnsForbidden()
    {
        SetupValidCustomerAndProgram();

        var handler = CreateHandler();
        var result = await handler.Handle(new EnrollCustomerInLoyaltyCommand(CustomerId, ProgramId, "someone-else"), CancellationToken.None);

        Assert.Equal(EnrollCustomerInLoyaltyOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyEnrolled_ReturnsAlreadyEnrolledWithExistingId()
    {
        SetupValidCustomerAndProgram();
        var existing = new LoyaltyAccount { CustomerId = CustomerId, LoyaltyProgramId = ProgramId, PointsBalance = 10 };
        existing.Id = 9;
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerAndProgramAsync(CustomerId, ProgramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new EnrollCustomerInLoyaltyCommand(CustomerId, ProgramId, OwnerId), CancellationToken.None);

        Assert.Equal(EnrollCustomerInLoyaltyOutcome.AlreadyEnrolled, result.Outcome);
        Assert.Equal(9, result.LoyaltyAccountId);
    }

    [Fact]
    public async Task Handle_ValidCommand_EnrollsWithZeroBalance()
    {
        SetupValidCustomerAndProgram();
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerAndProgramAsync(CustomerId, ProgramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyAccount?)null);
        _loyaltyAccountRepository.Setup(r => r.Add(It.IsAny<LoyaltyAccount>())).Callback<LoyaltyAccount>(a => a.Id = 3);

        var handler = CreateHandler();
        var result = await handler.Handle(new EnrollCustomerInLoyaltyCommand(CustomerId, ProgramId, OwnerId), CancellationToken.None);

        Assert.Equal(EnrollCustomerInLoyaltyOutcome.Enrolled, result.Outcome);
        Assert.Equal(3, result.LoyaltyAccountId);
        _loyaltyAccountRepository.Verify(r => r.Add(It.Is<LoyaltyAccount>(a => a.PointsBalance == 0)), Times.Once);
    }
}

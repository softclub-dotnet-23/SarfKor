using Application.Abstractions;
using Application.Loyalty.Commands.CreateLoyaltyProgram;
using Domain.Loyalty;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CreateLoyaltyProgramCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<ILoyaltyProgramRepository> _loyaltyProgramRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateLoyaltyProgramCommandHandler CreateHandler() => new(_storeRepository.Object, _storeAccessAuthorizer.Object, _loyaltyProgramRepository.Object, _unitOfWork.Object);

    private static CreateLoyaltyProgramCommand ValidCommand() => new(StoreId, PointsPerCurrencyUnit: 1, RedemptionRate: 0.1m, PerformedByUserId: OwnerId);

    private void SetupOwnedStore()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateLoyaltyProgramOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(CreateLoyaltyProgramOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ProgramAlreadyExists_ReturnsAlreadyExists()
    {
        SetupOwnedStore();
        _loyaltyProgramRepository
            .Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoyaltyProgram { StoreId = StoreId, PointsPerCurrencyUnit = 1, RedemptionRate = 0.1m, IsActive = true });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateLoyaltyProgramOutcome.AlreadyExists, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProgram()
    {
        SetupOwnedStore();
        _loyaltyProgramRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((LoyaltyProgram?)null);
        _loyaltyProgramRepository.Setup(r => r.Add(It.IsAny<LoyaltyProgram>())).Callback<LoyaltyProgram>(p => p.Id = 2);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(CreateLoyaltyProgramOutcome.Created, result.Outcome);
        Assert.Equal(2, result.LoyaltyProgramId);
    }
}

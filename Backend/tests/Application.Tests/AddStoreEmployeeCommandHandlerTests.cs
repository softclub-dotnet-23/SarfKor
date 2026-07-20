using Application.Abstractions;
using Application.Stores.Commands.AddStoreEmployee;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class AddStoreEmployeeCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AddStoreEmployeeCommandHandler CreateHandler() => new(_storeRepository.Object, _storeEmployeeRepository.Object, _unitOfWork.Object);

    private static AddStoreEmployeeCommand ValidCommand() => new(StoreId, "cashier-1", StoreEmployeeRole.Cashier, OwnerId);

    private void SetupOwnedStore() =>
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_UserAlreadyEmployedAtStore_ReturnsAlreadyEmployed()
    {
        SetupOwnedStore();
        _storeEmployeeRepository
            .Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StoreEmployee { StoreId = StoreId, UserId = "cashier-1", Role = StoreEmployeeRole.Cashier, AddedAt = DateTimeOffset.UtcNow }]);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.AlreadyEmployed, result.Outcome);
        _storeEmployeeRepository.Verify(r => r.Add(It.IsAny<StoreEmployee>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsEmployee()
    {
        SetupOwnedStore();
        _storeEmployeeRepository.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _storeEmployeeRepository.Setup(r => r.Add(It.IsAny<StoreEmployee>())).Callback<StoreEmployee>(e => e.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(AddStoreEmployeeOutcome.Added, result.Outcome);
        Assert.Equal(1, result.StoreEmployeeId);
    }
}

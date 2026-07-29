using Application.Abstractions;
using Application.Stores.Commands.RemoveStoreEmployee;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RemoveStoreEmployeeCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int EmployeeId = 1;

    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RemoveStoreEmployeeCommandHandler CreateHandler() => new(_storeEmployeeRepository.Object, _storeRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsNotFound()
    {
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync((StoreEmployee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveStoreEmployeeCommand(EmployeeId, OwnerId), CancellationToken.None);

        Assert.Equal(RemoveStoreEmployeeOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotStoreOwner_ReturnsForbidden()
    {
        _storeEmployeeRepository
            .Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoreEmployee { StoreId = StoreId, UserId = "cashier-1", Role = StoreEmployeeRole.Cashier, AddedAt = DateTimeOffset.UtcNow });
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveStoreEmployeeCommand(EmployeeId, "someone-else"), CancellationToken.None);

        Assert.Equal(RemoveStoreEmployeeOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesEmployee()
    {
        var employee = new StoreEmployee { StoreId = StoreId, UserId = "cashier-1", Role = StoreEmployeeRole.Cashier, AddedAt = DateTimeOffset.UtcNow };
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveStoreEmployeeCommand(EmployeeId, OwnerId), CancellationToken.None);

        Assert.Equal(RemoveStoreEmployeeOutcome.Removed, result.Outcome);
        _storeEmployeeRepository.Verify(r => r.Remove(employee), Times.Once);
    }
}

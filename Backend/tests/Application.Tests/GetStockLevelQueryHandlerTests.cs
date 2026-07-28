using Application.Abstractions;
using Application.Inventory.Queries.GetStockLevel;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetStockLevelQueryHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();

    public GetStockLevelQueryHandlerTests() =>
        _storeEmployeeRepository
            .Setup(r => r.IsEmployeeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

    private GetStockLevelQueryHandler CreateHandler() =>
        new(_storeRepository.Object, _storeEmployeeRepository.Object, _stockLevelRepository.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockLevelQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Equal(GetStockLevelOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockLevelQuery(StoreId, "someone-else"), CancellationToken.None);

        Assert.Equal(GetStockLevelOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsStockLevels()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _stockLevelRepository
            .Setup(r => r.GetByStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StockLevel { ProductId = 1, StoreId = StoreId, Quantity = 7 }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockLevelQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Equal(GetStockLevelOutcome.Found, result.Outcome);
        Assert.Single(result.Levels!);
        Assert.Equal(7, result.Levels![0].Quantity);
    }
}

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
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();

    private GetStockLevelQueryHandler CreateHandler() =>
        new(_storeRepository.Object, _storeAccessAuthorizer.Object, _stockLevelRepository.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockLevelQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Equal(GetStockLevelOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockLevelQuery(StoreId, "someone-else"), CancellationToken.None);

        Assert.Equal(GetStockLevelOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsStockLevels()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
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

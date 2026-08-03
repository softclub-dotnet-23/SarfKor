using Application.Abstractions;
using Application.Inventory.Queries.GetReorderAlerts;
using Domain.Inventory;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetReorderAlertsQueryHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IReorderRuleRepository> _reorderRuleRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();

    private GetReorderAlertsQueryHandler CreateHandler() =>
        new(_storeRepository.Object, _storeAccessAuthorizer.Object, _reorderRuleRepository.Object, _stockLevelRepository.Object, _productRepository.Object);

    private void SetupOwnedStore()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetReorderAlertsQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Equal(GetReorderAlertsOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_CallerIsNotStoreOwner_ReturnsForbidden()
    {
        SetupOwnedStore();

        var handler = CreateHandler();
        var result = await handler.Handle(new GetReorderAlertsQuery(StoreId, "someone-else"), CancellationToken.None);

        Assert.Equal(GetReorderAlertsOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_StockAboveThreshold_IsNotFlagged()
    {
        SetupOwnedStore();
        _reorderRuleRepository
            .Setup(r => r.GetActiveByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ReorderRule { StoreId = StoreId, ProductId = 1, ThresholdQuantity = 5, ReorderQuantity = 20, IsActive = true }]);
        _stockLevelRepository
            .Setup(r => r.GetByStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StockLevel { ProductId = 1, StoreId = StoreId, Quantity = 10 }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetReorderAlertsQuery(StoreId, OwnerId), CancellationToken.None);

        Assert.Empty(result.Alerts!);
    }

    [Fact]
    public async Task Handle_StockAtOrBelowThreshold_IsFlagged()
    {
        SetupOwnedStore();
        _reorderRuleRepository
            .Setup(r => r.GetActiveByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ReorderRule { StoreId = StoreId, ProductId = 1, ThresholdQuantity = 5, ReorderQuantity = 20, IsActive = true }]);
        _stockLevelRepository
            .Setup(r => r.GetByStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StockLevel { ProductId = 1, StoreId = StoreId, Quantity = 3 }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetReorderAlertsQuery(StoreId, OwnerId), CancellationToken.None);

        var alert = Assert.Single(result.Alerts!);
        Assert.Equal(1, alert.ProductId);
        Assert.Equal(3, alert.CurrentQuantity);
    }

    [Fact]
    public async Task Handle_NoStockLevelRecordAtAll_TreatsQuantityAsZeroAndFlags()
    {
        SetupOwnedStore();
        _reorderRuleRepository
            .Setup(r => r.GetActiveByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ReorderRule { StoreId = StoreId, ProductId = 1, ThresholdQuantity = 5, ReorderQuantity = 20, IsActive = true }]);
        _stockLevelRepository
            .Setup(r => r.GetByStoreAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetReorderAlertsQuery(StoreId, OwnerId), CancellationToken.None);

        var alert = Assert.Single(result.Alerts!);
        Assert.Equal(0, alert.CurrentQuantity);
    }
}

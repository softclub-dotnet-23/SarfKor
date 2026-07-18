using Application.Abstractions;
using Application.Sales.Commands.ProcessSale;
using Domain.Inventory;
using Domain.Pricing;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;
using Xunit;

namespace Application.Tests;

public class ProcessSaleCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ProcessSaleCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
    }

    private ProcessSaleCommandHandler CreateHandler() => new(
        _storeRepository.Object,
        _productRepository.Object,
        _priceEntryRepository.Object,
        _saleTransactionRepository.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(999, OwnerId, "key-1", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, "someone-else", "key-1", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsExistingSaleAndDoesNotTouchStock()
    {
        var existingSale = new SaleTransaction
        {
            StoreId = StoreId,
            CashierUserId = OwnerId,
            IdempotencyKey = "key-1",
            Currency = "TJS",
            Status = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = [new SaleLineItem { ProductId = 1, Quantity = 2, UnitPriceAtSale = new Money(10, "TJS") }]
        };

        _saleTransactionRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(StoreId, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSale);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-1", "TJS", [new ProcessSaleLine(1, 2)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(existingSale.Id, result.SaleTransactionId);
        Assert.Equal(20m, result.TotalAmount);

        _stockLevelRepository.Verify(
            r => r.TryDecrementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _saleTransactionRepository.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductDoesNotExist_ReturnsProductNotFound()
    {
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-2", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.ProductNotFound, result.Outcome);
        Assert.Equal(1, result.FailedProductId);
    }

    [Fact]
    public async Task Handle_NoPriceEntry_ReturnsPriceNotFound()
    {
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceEntry?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-3", "TJS", [new ProcessSaleLine(1, 1)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.PriceNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsInsufficientStockAndDoesNotCreateSale()
    {
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = StoreId, Price = new Money(10, "TJS") });
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, StoreId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-4", "TJS", [new ProcessSaleLine(1, 5)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.InsufficientStock, result.Outcome);
        Assert.Equal(1, result.FailedProductId);
        _saleTransactionRepository.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidSale_ReturnsCompletedWithCorrectTotal()
    {
        _productRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = StoreId, Price = new Money(15, "TJS") });
        _stockLevelRepository
            .Setup(r => r.TryDecrementAsync(1, StoreId, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ProcessSaleCommand(StoreId, OwnerId, "key-5", "TJS", [new ProcessSaleLine(1, 3)]),
            CancellationToken.None);

        Assert.Equal(ProcessSaleOutcome.Completed, result.Outcome);
        Assert.Equal(45m, result.TotalAmount);
        _saleTransactionRepository.Verify(r => r.Add(It.IsAny<SaleTransaction>()), Times.Once);
        _stockMovementRepository.Verify(r => r.Add(It.IsAny<StockMovement>()), Times.Once);
    }
}

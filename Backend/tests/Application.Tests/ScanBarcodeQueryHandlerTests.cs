using Application.Abstractions;
using Application.Products.Queries.ScanBarcode;
using Domain.Pricing;
using Domain.Products;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ScanBarcodeQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();

    private ScanBarcodeQueryHandler CreateHandler() => new(
        _productRepository.Object,
        _priceEntryRepository.Object,
        _storeRepository.Object);

    [Fact]
    public async Task Handle_UnknownBarcode_ReturnsNull()
    {
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", null, null), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_KnownBarcode_ReturnsStoresSortedByPriceWhenNoLocationGiven()
    {
        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Test", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(15, "TJS") },
                new PriceEntry { ProductId = 1, StoreId = 20, Price = new Money(10, "TJS") }
            ]);

        _storeRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Store { Id = 10, OwnerUserId = "o1", Name = "Store A", Address = "A", Location = new GeoLocation(0, 0) },
                new Store { Id = 20, OwnerUserId = "o2", Name = "Store B", Address = "B", Location = new GeoLocation(0, 0) }
            ]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", null, null), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.ProductId);
        Assert.Equal(2, result.Stores.Count);
        Assert.Equal(20, result.Stores[0].StoreId);
        Assert.Equal(10m, result.Stores[0].Price);
    }

    [Fact]
    public async Task Handle_WithUserLocation_SortsByDistanceFirst()
    {
        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Test", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(10, "TJS") },
                new PriceEntry { ProductId = 1, StoreId = 20, Price = new Money(15, "TJS") }
            ]);

        _storeRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Store { Id = 10, OwnerUserId = "o1", Name = "Far, cheaper", Address = "A", Location = new GeoLocation(10, 10) },
                new Store { Id = 20, OwnerUserId = "o2", Name = "Near, pricier", Address = "B", Location = new GeoLocation(0.001, 0.001) }
            ]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", 0, 0), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(20, result!.Stores[0].StoreId);
    }
}

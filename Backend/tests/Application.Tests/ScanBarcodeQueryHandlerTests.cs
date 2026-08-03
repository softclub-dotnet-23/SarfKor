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
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();

    private ScanBarcodeQueryHandler CreateHandler() => new(
        _productRepository.Object,
        _priceEntryRepository.Object,
        _storeRepository.Object,
        _storeAccessAuthorizer.Object);

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
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Store { Id = 10, OwnerUserId = "o1", Name = "Far, cheaper", Address = "A", Location = new GeoLocation(10, 10) },
                new Store { Id = 20, OwnerUserId = "o2", Name = "Near, pricier", Address = "B", Location = new GeoLocation(0.001, 0.001) }
            ]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", 0, 0), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(20, result!.Stores[0].StoreId);
    }

    [Fact]
    public async Task Handle_StoreHasAPriceButIsPending_IsOmittedFromResults()
    {
        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Test", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(10, "TJS") }]);

        // GetApprovedByIdsAsync excludes Pending stores at the repository level — the mock simply
        // returns nothing for store 10, exactly as the real filtered query would.
        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", null, null), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Stores);
    }

    [Fact]
    public async Task Handle_CallerOwnsThePendingStore_StillSeesItsOwnPrice()
    {
        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Test", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(10, "TJS") }]);

        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var pendingStore = new Store { Id = 10, OwnerUserId = "owner-1", Name = "My Own Store", Address = "A", Location = new GeoLocation(0, 0) };
        _storeRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(pendingStore);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(10, "owner-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", null, null, "owner-1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.Stores);
        Assert.Equal(10, result.Stores[0].StoreId);
    }

    [Fact]
    public async Task Handle_CallerIsUnrelatedToThePendingStore_StillOmitsIt()
    {
        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Test", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository.Setup(r => r.GetByBarcodeAsync("1234567890128", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(10, "TJS") }]);

        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(10, "someone-else", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanBarcodeQuery("1234567890128", null, null, "someone-else"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Stores);
    }
}

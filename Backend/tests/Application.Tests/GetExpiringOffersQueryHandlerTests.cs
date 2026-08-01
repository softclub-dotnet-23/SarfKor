using Application.Abstractions;
using Application.Offers.Queries.GetExpiringOffers;
using Domain.Offers;
using Domain.Products;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetExpiringOffersQueryHandlerTests
{
    private readonly Mock<IExpiringOfferRepository> _expiringOfferRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();

    private GetExpiringOffersQueryHandler CreateHandler() => new(
        _expiringOfferRepository.Object,
        _productRepository.Object,
        _storeRepository.Object);

    [Fact]
    public async Task Handle_NoActiveOffers_ReturnsEmptyList()
    {
        _expiringOfferRepository
            .Setup(r => r.GetActiveAsync(null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpiringOffersQuery(null, null, null), CancellationToken.None);

        Assert.Empty(result.Offers);
    }

    [Fact]
    public async Task Handle_ActiveOffers_MapsProductAndStoreNames()
    {
        var offer = new ExpiringOffer
        {
            ProductId = 1,
            StoreId = 10,
            OriginalPrice = new Money(20, "TJS"),
            DiscountedPrice = new Money(15, "TJS"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            CreatedAt = DateTimeOffset.UtcNow
        };
        offer.Id = 1;

        _expiringOfferRepository
            .Setup(r => r.GetActiveAsync(null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([offer]);

        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Milk", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var store = new Store { Id = 10, OwnerUserId = "o1", Name = "Corner Shop", Address = "Addr", Location = new GeoLocation(0, 0) };
        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([store]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpiringOffersQuery(null, null, null), CancellationToken.None);

        Assert.Single(result.Offers);
        Assert.Equal("Milk", result.Offers[0].ProductName);
        Assert.Equal("Corner Shop", result.Offers[0].StoreName);
        Assert.Equal(15m, result.Offers[0].DiscountedPrice);
    }

    [Fact]
    public async Task Handle_OfferForDeletedProduct_IsSkippedRatherThanThrowing()
    {
        var offer = new ExpiringOffer
        {
            ProductId = 999,
            StoreId = 10,
            OriginalPrice = new Money(20, "TJS"),
            DiscountedPrice = new Money(15, "TJS"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _expiringOfferRepository
            .Setup(r => r.GetActiveAsync(null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([offer]);
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Store { Id = 10, OwnerUserId = "o1", Name = "Corner Shop", Address = "Addr", Location = new GeoLocation(0, 0) }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpiringOffersQuery(null, null, null), CancellationToken.None);

        Assert.Empty(result.Offers);
    }

    [Fact]
    public async Task Handle_OfferForPendingStore_IsSkippedRatherThanShown()
    {
        var offer = new ExpiringOffer
        {
            ProductId = 1,
            StoreId = 10,
            OriginalPrice = new Money(20, "TJS"),
            DiscountedPrice = new Money(15, "TJS"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _expiringOfferRepository
            .Setup(r => r.GetActiveAsync(null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([offer]);

        var product = new Product { Barcode = new Barcode("1234567890128"), Name = "Milk", CountryOfOrigin = "TJ" };
        product.Id = 1;
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        // GetApprovedByIdsAsync excludes Pending stores at the repository level.
        _storeRepository
            .Setup(r => r.GetApprovedByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpiringOffersQuery(null, null, null), CancellationToken.None);

        Assert.Empty(result.Offers);
    }
}

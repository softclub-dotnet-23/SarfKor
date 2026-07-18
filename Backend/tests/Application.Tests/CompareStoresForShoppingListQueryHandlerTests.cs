using Application.Abstractions;
using Application.Products.Queries.CompareStoresForShoppingList;
using Domain.Pricing;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class CompareStoresForShoppingListQueryHandlerTests
{
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();

    private CompareStoresForShoppingListQueryHandler CreateHandler() => new(_priceEntryRepository.Object, _storeRepository.Object);

    [Fact]
    public async Task Handle_StoreMissingOneOfTheRequestedProducts_IsExcluded()
    {
        // Store 10 has both product 1 and 2; store 20 has only product 1.
        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(10, "TJS") },
                new PriceEntry { ProductId = 1, StoreId = 20, Price = new Money(8, "TJS") }
            ]);
        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PriceEntry { ProductId = 2, StoreId = 10, Price = new Money(5, "TJS") }
            ]);

        _storeRepository
            .Setup(r => r.GetByIdsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(10) && !ids.Contains(20)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Store { Id = 10, OwnerUserId = "o1", Name = "Complete store", Address = "A", Location = new GeoLocation(0, 0) }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompareStoresForShoppingListQuery([1, 2], null, null), CancellationToken.None);

        Assert.Single(result.Stores);
        Assert.Equal(10, result.Stores[0].StoreId);
        Assert.Equal(15m, result.Stores[0].TotalPrice);
    }

    [Fact]
    public async Task Handle_MultipleCompleteStores_SortedByTotalPriceAscending()
    {
        _priceEntryRepository
            .Setup(r => r.GetLatestPerStoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PriceEntry { ProductId = 1, StoreId = 10, Price = new Money(20, "TJS") },
                new PriceEntry { ProductId = 1, StoreId = 20, Price = new Money(10, "TJS") }
            ]);

        _storeRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Store { Id = 10, OwnerUserId = "o1", Name = "Pricier", Address = "A", Location = new GeoLocation(0, 0) },
                new Store { Id = 20, OwnerUserId = "o2", Name = "Cheaper", Address = "B", Location = new GeoLocation(0, 0) }
            ]);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompareStoresForShoppingListQuery([1], null, null), CancellationToken.None);

        Assert.Equal(2, result.Stores.Count);
        Assert.Equal(20, result.Stores[0].StoreId);
        Assert.Equal(10m, result.Stores[0].TotalPrice);
    }
}

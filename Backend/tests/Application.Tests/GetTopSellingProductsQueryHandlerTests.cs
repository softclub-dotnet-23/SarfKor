using Application.Abstractions;
using Application.Products.Queries.GetTopSellingProducts;
using Domain.Products;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class GetTopSellingProductsQueryHandlerTests
{
    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();

    private GetTopSellingProductsQueryHandler CreateHandler() => new(_saleTransactionRepository.Object, _productRepository.Object);

    [Fact]
    public async Task Handle_MapsSummariesToProductNames_PreservingOrder()
    {
        _saleTransactionRepository
            .Setup(r => r.GetTopSellingProductsAsync(null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductSalesSummary(1, 50),
                new ProductSalesSummary(2, 30)
            ]);

        var product1 = new Product { Barcode = new Barcode("1111111111111"), Name = "Bread", CountryOfOrigin = "TJ" };
        product1.Id = 1;
        var product2 = new Product { Barcode = new Barcode("2222222222222"), Name = "Milk", CountryOfOrigin = "TJ" };
        product2.Id = 2;

        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product1, product2]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetTopSellingProductsQuery(null, 10), CancellationToken.None);

        Assert.Equal(2, result.Products.Count);
        Assert.Equal("Bread", result.Products[0].ProductName);
        Assert.Equal(50, result.Products[0].TotalQuantity);
        Assert.Equal("Milk", result.Products[1].ProductName);
    }

    [Fact]
    public async Task Handle_SummaryForDeletedProduct_IsSkippedRatherThanThrowing()
    {
        _saleTransactionRepository
            .Setup(r => r.GetTopSellingProductsAsync(null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductSalesSummary(999, 5)]);

        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetTopSellingProductsQuery(null, 10), CancellationToken.None);

        Assert.Empty(result.Products);
    }
}

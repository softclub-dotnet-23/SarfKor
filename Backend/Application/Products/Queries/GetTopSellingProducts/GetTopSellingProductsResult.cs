namespace Application.Products.Queries.GetTopSellingProducts;

public sealed record TopSellingProductDto(int ProductId, string ProductName, int TotalQuantity);

public sealed record GetTopSellingProductsResult(IReadOnlyList<TopSellingProductDto> Products);

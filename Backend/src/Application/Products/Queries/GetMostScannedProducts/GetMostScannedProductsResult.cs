namespace Application.Products.Queries.GetMostScannedProducts;

public sealed record MostScannedProductDto(int ProductId, string ProductName, int TotalScans);

public sealed record GetMostScannedProductsResult(IReadOnlyList<MostScannedProductDto> Products);

namespace Application.Products.Queries.SearchProducts;

public sealed record ProductSearchItemDto(
    int ProductId,
    string Name,
    string Barcode,
    string? BrandName,
    string? CategoryName,
    decimal? Price,
    string? Currency);

public sealed record SearchProductsResult(IReadOnlyList<ProductSearchItemDto> Items, int TotalCount);

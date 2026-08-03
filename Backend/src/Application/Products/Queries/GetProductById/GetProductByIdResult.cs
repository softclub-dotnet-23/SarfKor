namespace Application.Products.Queries.GetProductById;

public sealed record GetProductByIdResult(int ProductId, string ProductName, string Barcode, int CategoryId, int BrandId, string CountryOfOrigin);

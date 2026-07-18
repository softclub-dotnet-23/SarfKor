namespace Application.Products.Queries.GetTopSellingProducts;

public sealed record GetTopSellingProductsQuery(int? StoreId, int Limit);

namespace Application.Catalog.Queries.GetProductBundles;

public sealed record ProductBundleItemDto(int ProductId, int Quantity);

public sealed record ProductBundleDto(int ProductBundleId, string Name, decimal BundlePrice, string Currency, IReadOnlyList<ProductBundleItemDto> Items);

public sealed record GetProductBundlesResult(IReadOnlyList<ProductBundleDto> Bundles);

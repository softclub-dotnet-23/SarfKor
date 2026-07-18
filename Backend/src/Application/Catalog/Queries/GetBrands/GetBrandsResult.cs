namespace Application.Catalog.Queries.GetBrands;

public sealed record BrandDto(int BrandId, string Name);

public sealed record GetBrandsResult(IReadOnlyList<BrandDto> Brands);

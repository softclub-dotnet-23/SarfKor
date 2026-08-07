using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Queries.GetBrands;

public sealed class GetBrandsQueryHandler(IBrandRepository brandRepository, IProductRepository productRepository)
    : IQueryHandler<GetBrandsQuery, GetBrandsResult>
{
    public async Task<GetBrandsResult> Handle(GetBrandsQuery query, CancellationToken cancellationToken)
    {
        var brands = await brandRepository.GetAllAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            brands = brands.Where(b => b.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var counts = await productRepository.CountByBrandIdsAsync(brands.Select(b => b.Id).ToList(), cancellationToken);
        return new GetBrandsResult(brands.Select(b => new BrandDto(b.Id, b.Name, counts.GetValueOrDefault(b.Id))).ToList());
    }
}

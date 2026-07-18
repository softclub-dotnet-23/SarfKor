using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Queries.GetBrands;

public sealed class GetBrandsQueryHandler(IBrandRepository brandRepository) : IQueryHandler<GetBrandsQuery, GetBrandsResult>
{
    public async Task<GetBrandsResult> Handle(GetBrandsQuery query, CancellationToken cancellationToken)
    {
        var brands = await brandRepository.GetAllAsync(cancellationToken);
        return new GetBrandsResult(brands.Select(b => new BrandDto(b.Id, b.Name)).ToList());
    }
}

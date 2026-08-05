using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Queries.GetBrandDuplicateCandidates;

public sealed class GetBrandDuplicateCandidatesQueryHandler(IBrandRepository brandRepository, IProductRepository productRepository)
    : IQueryHandler<GetBrandDuplicateCandidatesQuery, GetBrandDuplicateCandidatesResult>
{
    public async Task<GetBrandDuplicateCandidatesResult> Handle(GetBrandDuplicateCandidatesQuery query, CancellationToken cancellationToken)
    {
        var brands = await brandRepository.GetAllAsync(cancellationToken);
        var counts = await productRepository.CountByBrandIdsAsync(brands.Select(b => b.Id).ToList(), cancellationToken);

        var groups = brands
            .GroupBy(b => BrandNameNormalizer.Normalize(b.Name))
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateBrandGroupDto(
                g.Key,
                g.Select(b => new DuplicateBrandDto(b.Id, b.Name, counts.GetValueOrDefault(b.Id)))
                    .OrderByDescending(b => b.ProductCount)
                    .ToList()))
            .OrderByDescending(g => g.Brands.Sum(b => b.ProductCount))
            .ToList();

        return new GetBrandDuplicateCandidatesResult(groups);
    }
}

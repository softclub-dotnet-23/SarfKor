using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Queries.GetProductBundles;

public sealed class GetProductBundlesQueryHandler(IProductBundleRepository productBundleRepository) : IQueryHandler<GetProductBundlesQuery, GetProductBundlesResult>
{
    public async Task<GetProductBundlesResult> Handle(GetProductBundlesQuery query, CancellationToken cancellationToken)
    {
        var bundles = await productBundleRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);

        var dtos = bundles
            .Select(b => new ProductBundleDto(
                b.Id,
                b.Name,
                b.BundlePrice.Amount,
                b.BundlePrice.Currency,
                b.Items.Select(i => new ProductBundleItemDto(i.ProductId, i.Quantity)).ToList()))
            .ToList();

        return new GetProductBundlesResult(dtos);
    }
}

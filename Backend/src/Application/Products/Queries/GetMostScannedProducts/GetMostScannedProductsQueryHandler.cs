using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.GetMostScannedProducts;

public sealed class GetMostScannedProductsQueryHandler(
    IScanRepository scanRepository,
    IProductRepository productRepository) : IQueryHandler<GetMostScannedProductsQuery, GetMostScannedProductsResult>
{
    public async Task<GetMostScannedProductsResult> Handle(GetMostScannedProductsQuery query, CancellationToken cancellationToken)
    {
        var summaries = await scanRepository.GetMostScannedAsync(query.Limit, cancellationToken);
        var products = await productRepository.GetByIdsAsync(summaries.Select(s => s.ProductId).ToList(), cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        var dtos = summaries
            .Where(s => productsById.ContainsKey(s.ProductId))
            .Select(s => new MostScannedProductDto(s.ProductId, productsById[s.ProductId].Name, s.TotalScans))
            .ToList();

        return new GetMostScannedProductsResult(dtos);
    }
}

using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsQueryHandler(
    ISaleTransactionRepository saleTransactionRepository,
    IProductRepository productRepository) : IQueryHandler<GetTopSellingProductsQuery, GetTopSellingProductsResult>
{
    public async Task<GetTopSellingProductsResult> Handle(GetTopSellingProductsQuery query, CancellationToken cancellationToken)
    {
        var summaries = await saleTransactionRepository.GetTopSellingProductsAsync(query.StoreId, query.Limit, cancellationToken);
        var products = await productRepository.GetByIdsAsync(summaries.Select(s => s.ProductId).ToList(), cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        var dtos = summaries
            .Where(s => productsById.ContainsKey(s.ProductId))
            .Select(s => new TopSellingProductDto(s.ProductId, productsById[s.ProductId].Name, s.TotalQuantity))
            .ToList();

        return new GetTopSellingProductsResult(dtos);
    }
}

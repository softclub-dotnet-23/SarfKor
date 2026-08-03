using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IProductRepository productRepository) : IQueryHandler<GetProductByIdQuery, GetProductByIdResult?>
{
    public async Task<GetProductByIdResult?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
            return null;

        return new GetProductByIdResult(product.Id, product.Name, product.Barcode.Value, product.CategoryId, product.BrandId, product.CountryOfOrigin);
    }
}

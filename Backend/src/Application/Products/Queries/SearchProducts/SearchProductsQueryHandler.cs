using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler(
    IProductRepository productRepository,
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IPriceEntryRepository priceEntryRepository) : IQueryHandler<SearchProductsQuery, SearchProductsResult>
{
    public async Task<SearchProductsResult> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await productRepository.SearchAsync(
            query.Search, query.CategoryId, query.Skip, query.Take, cancellationToken);

        // Brand/Category are small, platform-wide reference tables (the same assumption
        // GetBrandsQueryHandler/GetCategoriesQueryHandler already make) — one full fetch each,
        // turned into id->name lookups, is cheaper than a per-row join for a page of ~20-50 rows.
        var brandsById = (await brandRepository.GetAllAsync(cancellationToken)).ToDictionary(b => b.Id, b => b.Name);
        var categoriesById = (await categoryRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        Dictionary<int, (decimal Amount, string Currency)> pricesByProductId = [];
        if (query.StoreId.HasValue && products.Count > 0)
        {
            var entries = await priceEntryRepository.GetLatestForStoreForProductsAsync(
                query.StoreId.Value, products.Select(p => p.Id).ToList(), cancellationToken);
            pricesByProductId = entries.ToDictionary(e => e.ProductId, e => (e.Price.Amount, e.Price.Currency));
        }

        var items = products.Select(p =>
        {
            var hasPrice = pricesByProductId.TryGetValue(p.Id, out var price);
            return new ProductSearchItemDto(
                p.Id,
                p.Name,
                p.Barcode.Value,
                brandsById.GetValueOrDefault(p.BrandId),
                categoriesById.GetValueOrDefault(p.CategoryId),
                hasPrice ? price.Amount : null,
                hasPrice ? price.Currency : null);
        }).ToList();

        return new SearchProductsResult(items, totalCount);
    }
}

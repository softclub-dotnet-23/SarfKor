using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler(ICategoryRepository categoryRepository) : IQueryHandler<GetCategoriesQuery, GetCategoriesResult>
{
    public async Task<GetCategoriesResult> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        return new GetCategoriesResult(categories.Select(c => new CategoryDto(c.Id, c.Name, c.ParentCategoryId)).ToList());
    }
}

namespace Application.Catalog.Queries.GetCategories;

public sealed record CategoryDto(int CategoryId, string Name, int? ParentCategoryId);

public sealed record GetCategoriesResult(IReadOnlyList<CategoryDto> Categories);

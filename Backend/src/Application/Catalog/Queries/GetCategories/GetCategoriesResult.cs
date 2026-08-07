namespace Application.Catalog.Queries.GetCategories;

public sealed record CategoryDto(int CategoryId, string Name, int? ParentCategoryId, int DisplayOrder, bool IsHidden);

public sealed record GetCategoriesResult(IReadOnlyList<CategoryDto> Categories);

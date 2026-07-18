namespace Application.Catalog.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, int? ParentCategoryId);

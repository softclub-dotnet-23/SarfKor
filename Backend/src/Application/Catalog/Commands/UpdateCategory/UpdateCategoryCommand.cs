namespace Application.Catalog.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    int CategoryId, string Name, int? ParentCategoryId, int DisplayOrder, bool IsHidden,
    string PerformedByUserId, string? PerformedByIpAddress = null);

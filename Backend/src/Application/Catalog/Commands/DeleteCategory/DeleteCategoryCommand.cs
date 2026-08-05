namespace Application.Catalog.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(int CategoryId, string PerformedByUserId, string? PerformedByIpAddress = null);

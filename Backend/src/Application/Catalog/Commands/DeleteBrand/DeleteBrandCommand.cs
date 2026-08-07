namespace Application.Catalog.Commands.DeleteBrand;

public sealed record DeleteBrandCommand(int BrandId, string PerformedByUserId, string? PerformedByIpAddress = null);

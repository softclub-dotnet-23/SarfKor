namespace Application.Catalog.Commands.UpdateBrand;

public sealed record UpdateBrandCommand(int BrandId, string Name, string PerformedByUserId, string? PerformedByIpAddress = null);

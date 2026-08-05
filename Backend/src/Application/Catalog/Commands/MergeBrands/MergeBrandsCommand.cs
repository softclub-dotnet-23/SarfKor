namespace Application.Catalog.Commands.MergeBrands;

public sealed record MergeBrandsCommand(int TargetBrandId, IReadOnlyList<int> SourceBrandIds, string PerformedByUserId, string? PerformedByIpAddress = null);

namespace Application.Catalog.Queries.GetBrandDuplicateCandidates;

public sealed record DuplicateBrandDto(int BrandId, string Name, int ProductCount);
public sealed record DuplicateBrandGroupDto(string NormalizedKey, IReadOnlyList<DuplicateBrandDto> Brands);

public sealed record GetBrandDuplicateCandidatesResult(IReadOnlyList<DuplicateBrandGroupDto> Groups);

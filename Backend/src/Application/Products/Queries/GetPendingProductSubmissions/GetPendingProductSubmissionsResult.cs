namespace Application.Products.Queries.GetPendingProductSubmissions;

public sealed record ProductSubmissionDto(
    int SubmissionId,
    string Barcode,
    string Name,
    int CategoryId,
    int BrandId,
    string CountryOfOrigin,
    string SubmittedByUserId,
    DateTimeOffset CreatedAt);

public sealed record GetPendingProductSubmissionsResult(IReadOnlyList<ProductSubmissionDto> Submissions);

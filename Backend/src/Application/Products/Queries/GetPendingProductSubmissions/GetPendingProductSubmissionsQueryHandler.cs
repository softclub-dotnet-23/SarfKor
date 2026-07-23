using Application.Abstractions;
using Application.Common;

namespace Application.Products.Queries.GetPendingProductSubmissions;

public sealed class GetPendingProductSubmissionsQueryHandler(IProductSubmissionRepository productSubmissionRepository)
    : IQueryHandler<GetPendingProductSubmissionsQuery, GetPendingProductSubmissionsResult>
{
    public async Task<GetPendingProductSubmissionsResult> Handle(GetPendingProductSubmissionsQuery query, CancellationToken cancellationToken)
    {
        var submissions = await productSubmissionRepository.GetPendingAsync(cancellationToken);
        var dtos = submissions
            .Select(s => new ProductSubmissionDto(
                s.Id,
                s.Barcode.Value,
                s.Name,
                s.CategoryId,
                s.BrandId,
                s.CountryOfOrigin,
                s.SubmittedByUserId,
                s.CreatedAt))
            .ToList();
        return new GetPendingProductSubmissionsResult(dtos);
    }
}

using Domain.Products;

namespace Application.Abstractions;

public interface IProductSubmissionRepository
{
    Task<ProductSubmission?> GetByIdAsync(int productSubmissionId, CancellationToken cancellationToken);
}

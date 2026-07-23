using Domain.Products;

namespace Application.Abstractions;

public interface IProductSubmissionRepository
{
    Task<ProductSubmission?> GetByIdAsync(int productSubmissionId, CancellationToken cancellationToken);
    Task<ProductSubmission?> GetPendingByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSubmission>> GetPendingAsync(CancellationToken cancellationToken);
    void Add(ProductSubmission submission);
}

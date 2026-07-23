using Application.Abstractions;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductSubmissionRepository(AppDbContext dbContext) : IProductSubmissionRepository
{
    public Task<ProductSubmission?> GetByIdAsync(int productSubmissionId, CancellationToken cancellationToken) =>
        dbContext.ProductSubmissions.FirstOrDefaultAsync(p => p.Id == productSubmissionId, cancellationToken);

    public Task<ProductSubmission?> GetPendingByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        dbContext.ProductSubmissions.FirstOrDefaultAsync(
            p => p.Barcode.Value == barcode && p.Status == ProductSubmissionStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<ProductSubmission>> GetPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.ProductSubmissions.Where(p => p.Status == ProductSubmissionStatus.Pending).ToListAsync(cancellationToken);

    public void Add(ProductSubmission submission) => dbContext.ProductSubmissions.Add(submission);
}

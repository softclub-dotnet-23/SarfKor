using Application.Abstractions;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductSubmissionRepository(AppDbContext dbContext) : IProductSubmissionRepository
{
    public Task<ProductSubmission?> GetByIdAsync(int productSubmissionId, CancellationToken cancellationToken) =>
        dbContext.ProductSubmissions.FirstOrDefaultAsync(p => p.Id == productSubmissionId, cancellationToken);

    public async Task<IReadOnlyList<ProductSubmission>> GetPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.ProductSubmissions.Where(p => p.Status == ProductSubmissionStatus.Pending).ToListAsync(cancellationToken);
}

using Application.Abstractions;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ProductSubmissionRepository(AppDbContext dbContext) : IProductSubmissionRepository
{
    public Task<ProductSubmission?> GetByIdAsync(int productSubmissionId, CancellationToken cancellationToken) =>
        dbContext.ProductSubmissions.FirstOrDefaultAsync(p => p.Id == productSubmissionId, cancellationToken);
}

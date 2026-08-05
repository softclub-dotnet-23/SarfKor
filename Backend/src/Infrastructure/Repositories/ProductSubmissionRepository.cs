using Application.Abstractions;
using Domain.Products;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class ProductSubmissionRepository(AppDbContext dbContext) : IProductSubmissionRepository
{
    public void Add(ProductSubmission submission) => dbContext.ProductSubmissions.Add(submission);
}

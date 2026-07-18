using Application.Abstractions;
using Domain.Feedback;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ReviewRepository(AppDbContext dbContext) : IReviewRepository
{
    public Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken) =>
        dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByProductIdAsync(int productId, CancellationToken cancellationToken) =>
        await dbContext.Reviews.Where(r => r.ProductId == productId).ToListAsync(cancellationToken);

    public void Add(Review review) => dbContext.Reviews.Add(review);
}

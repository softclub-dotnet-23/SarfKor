using Domain.Feedback;

namespace Application.Abstractions;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int reviewId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Review>> GetByProductIdAsync(int productId, CancellationToken cancellationToken);
    void Add(Review review);
}

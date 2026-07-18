using Application.Abstractions;
using Application.Common;

namespace Application.Feedback.Queries.GetReviews;

public sealed class GetReviewsQueryHandler(IReviewRepository reviewRepository) : IQueryHandler<GetReviewsQuery, GetReviewsResult>
{
    public async Task<GetReviewsResult> Handle(GetReviewsQuery query, CancellationToken cancellationToken)
    {
        var reviews = await reviewRepository.GetByProductIdAsync(query.ProductId, cancellationToken);
        return new GetReviewsResult(reviews.Select(r => new ReviewDto(r.Id, r.UserId, r.StoreId, r.Rating, r.Comment, r.CreatedAt)).ToList());
    }
}

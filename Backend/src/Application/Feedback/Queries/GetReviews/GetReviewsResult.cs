namespace Application.Feedback.Queries.GetReviews;

public sealed record ReviewDto(int ReviewId, string UserId, int? StoreId, int Rating, string Comment, DateTimeOffset CreatedAt);

public sealed record GetReviewsResult(IReadOnlyList<ReviewDto> Reviews);

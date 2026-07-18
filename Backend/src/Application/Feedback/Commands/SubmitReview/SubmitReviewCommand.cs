namespace Application.Feedback.Commands.SubmitReview;

public sealed record SubmitReviewCommand(string UserId, int ProductId, int? StoreId, int Rating, string Comment);

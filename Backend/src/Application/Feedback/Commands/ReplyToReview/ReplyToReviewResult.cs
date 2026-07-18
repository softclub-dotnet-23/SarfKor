namespace Application.Feedback.Commands.ReplyToReview;

public enum ReplyToReviewOutcome
{
    Replied,
    ReviewNotFound,
    Forbidden
}

public sealed record ReplyToReviewResult(ReplyToReviewOutcome Outcome, int? ReplyId);

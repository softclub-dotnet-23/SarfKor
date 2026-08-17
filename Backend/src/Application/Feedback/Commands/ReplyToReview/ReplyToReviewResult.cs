namespace Application.Feedback.Commands.ReplyToReview;

public enum ReplyToReviewOutcome
{
    Replied,
    ReviewNotFound,
    Forbidden,
    SubscriptionInactive
}

public sealed record ReplyToReviewResult(ReplyToReviewOutcome Outcome, int? ReplyId);

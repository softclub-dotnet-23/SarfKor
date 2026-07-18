namespace Application.Feedback.Commands.ReplyToReview;

public sealed record ReplyToReviewCommand(int ReviewId, string StorePartnerUserId, string Message);

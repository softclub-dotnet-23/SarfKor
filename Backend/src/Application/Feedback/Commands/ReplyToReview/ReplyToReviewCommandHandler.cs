using Application.Abstractions;
using Application.Common;
using Domain.Feedback;

namespace Application.Feedback.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandHandler(
    IReviewRepository reviewRepository,
    IReviewReplyRepository reviewReplyRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<ReplyToReviewCommand, ReplyToReviewResult>
{
    public async Task<ReplyToReviewResult> Handle(ReplyToReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
            return new ReplyToReviewResult(ReplyToReviewOutcome.ReviewNotFound, null);

        // A review not tied to a specific store has no owner who could legitimately reply to it.
        if (review.StoreId is null || !await storeAccessAuthorizer.IsOwnerAsync(review.StoreId.Value, command.StorePartnerUserId, cancellationToken))
            return new ReplyToReviewResult(ReplyToReviewOutcome.Forbidden, null);

        var reply = new ReviewReply
        {
            ReviewId = command.ReviewId,
            StorePartnerUserId = command.StorePartnerUserId,
            Message = command.Message,
            CreatedAt = DateTimeOffset.UtcNow
        };

        reviewReplyRepository.Add(reply);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReplyToReviewResult(ReplyToReviewOutcome.Replied, reply.Id);
    }
}

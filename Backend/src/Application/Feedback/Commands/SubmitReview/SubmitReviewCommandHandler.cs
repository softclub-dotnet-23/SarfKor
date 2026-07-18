using Application.Abstractions;
using Application.Common;
using Domain.Feedback;

namespace Application.Feedback.Commands.SubmitReview;

public sealed class SubmitReviewCommandHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitReviewCommand, SubmitReviewResult>
{
    public async Task<SubmitReviewResult> Handle(SubmitReviewCommand command, CancellationToken cancellationToken)
    {
        var review = new Review
        {
            UserId = command.UserId,
            ProductId = command.ProductId,
            StoreId = command.StoreId,
            Rating = command.Rating,
            Comment = command.Comment,
            CreatedAt = DateTimeOffset.UtcNow
        };

        reviewRepository.Add(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitReviewResult(review.Id);
    }
}

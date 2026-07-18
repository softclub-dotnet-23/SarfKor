using Application.Abstractions;
using Application.Feedback.Commands.ReplyToReview;
using Domain.Feedback;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ReplyToReviewCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int ReviewId = 1;

    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<IReviewReplyRepository> _reviewReplyRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ReplyToReviewCommandHandler CreateHandler() => new(
        _reviewRepository.Object,
        _reviewReplyRepository.Object,
        _storeRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_ReviewNotFound_ReturnsReviewNotFound()
    {
        _reviewRepository.Setup(r => r.GetByIdAsync(ReviewId, It.IsAny<CancellationToken>())).ReturnsAsync((Review?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReplyToReviewCommand(ReviewId, OwnerId, "Thanks!"), CancellationToken.None);

        Assert.Equal(ReplyToReviewOutcome.ReviewNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ReviewHasNoStore_ReturnsForbidden()
    {
        _reviewRepository
            .Setup(r => r.GetByIdAsync(ReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Review { UserId = "user-1", ProductId = 1, StoreId = null, Rating = 5, Comment = "Great", CreatedAt = DateTimeOffset.UtcNow });

        var handler = CreateHandler();
        var result = await handler.Handle(new ReplyToReviewCommand(ReviewId, OwnerId, "Thanks!"), CancellationToken.None);

        Assert.Equal(ReplyToReviewOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotStoreOwner_ReturnsForbidden()
    {
        _reviewRepository
            .Setup(r => r.GetByIdAsync(ReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Review { UserId = "user-1", ProductId = 1, StoreId = 2, Rating = 5, Comment = "Great", CreatedAt = DateTimeOffset.UtcNow });
        _storeRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new ReplyToReviewCommand(ReviewId, "someone-else", "Thanks!"), CancellationToken.None);

        Assert.Equal(ReplyToReviewOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_StoreOwner_CreatesReply()
    {
        _reviewRepository
            .Setup(r => r.GetByIdAsync(ReviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Review { UserId = "user-1", ProductId = 1, StoreId = 2, Rating = 5, Comment = "Great", CreatedAt = DateTimeOffset.UtcNow });
        _storeRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _reviewReplyRepository.Setup(r => r.Add(It.IsAny<ReviewReply>())).Callback<ReviewReply>(x => x.Id = 4);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReplyToReviewCommand(ReviewId, OwnerId, "Thanks!"), CancellationToken.None);

        Assert.Equal(ReplyToReviewOutcome.Replied, result.Outcome);
        Assert.Equal(4, result.ReplyId);
    }
}

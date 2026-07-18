using Application.Abstractions;
using Application.Feedback.Queries.GetReviews;
using Domain.Feedback;
using Moq;

namespace Application.Tests;

public class GetReviewsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsReviewsForProduct()
    {
        var repo = new Mock<IReviewRepository>();
        repo.Setup(r => r.GetByProductIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync([
            new Review { UserId = "user-1", ProductId = 5, Rating = 4, Comment = "Good", CreatedAt = DateTimeOffset.UtcNow }
        ]);

        var handler = new GetReviewsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetReviewsQuery(5), CancellationToken.None);

        Assert.Single(result.Reviews);
        Assert.Equal(4, result.Reviews[0].Rating);
    }
}

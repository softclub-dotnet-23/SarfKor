using Application.Abstractions;
using Application.Feedback.Commands.SubmitReview;
using Domain.Feedback;
using Moq;

namespace Application.Tests;

public class SubmitReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesReview_AndReturnsItsId()
    {
        var repo = new Mock<IReviewRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.Add(It.IsAny<Review>())).Callback<Review>(r => r.Id = 3);

        var handler = new SubmitReviewCommandHandler(repo.Object, uow.Object);
        var result = await handler.Handle(new SubmitReviewCommand("user-1", 5, 2, 4, "Good product"), CancellationToken.None);

        Assert.Equal(3, result.ReviewId);
        repo.Verify(r => r.Add(It.Is<Review>(x => x.Rating == 4 && x.ProductId == 5 && x.StoreId == 2)), Times.Once);
    }
}

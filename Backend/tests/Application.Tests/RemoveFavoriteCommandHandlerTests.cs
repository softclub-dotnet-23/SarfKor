using Application.Abstractions;
using Application.Engagement.Commands.RemoveFavorite;
using Domain.Engagement;
using Moq;

namespace Application.Tests;

public class RemoveFavoriteCommandHandlerTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RemoveFavoriteCommandHandler CreateHandler() => new(_favoriteRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NotFavorited_ReturnsNotFound()
    {
        _favoriteRepository
            .Setup(r => r.GetAsync("user-1", FavoriteType.Store, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveFavoriteCommand("user-1", FavoriteType.Store, 2), CancellationToken.None);

        Assert.Equal(RemoveFavoriteOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_Favorited_RemovesIt()
    {
        var favorite = new Favorite { UserId = "user-1", Type = FavoriteType.Store, EntityId = 2, CreatedAt = DateTimeOffset.UtcNow };
        _favoriteRepository
            .Setup(r => r.GetAsync("user-1", FavoriteType.Store, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(favorite);

        var handler = CreateHandler();
        var result = await handler.Handle(new RemoveFavoriteCommand("user-1", FavoriteType.Store, 2), CancellationToken.None);

        Assert.Equal(RemoveFavoriteOutcome.Removed, result.Outcome);
        _favoriteRepository.Verify(r => r.Remove(favorite), Times.Once);
    }
}

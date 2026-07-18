using Application.Abstractions;
using Application.Engagement.Queries.GetFavorites;
using Domain.Engagement;
using Moq;

namespace Application.Tests;

public class GetFavoritesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsersFavorites()
    {
        var repo = new Mock<IFavoriteRepository>();
        repo.Setup(r => r.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync([
            new Favorite { UserId = "user-1", Type = FavoriteType.Product, EntityId = 5, CreatedAt = DateTimeOffset.UtcNow }
        ]);

        var handler = new GetFavoritesQueryHandler(repo.Object);
        var result = await handler.Handle(new GetFavoritesQuery("user-1"), CancellationToken.None);

        Assert.Single(result.Favorites);
        Assert.Equal(FavoriteType.Product, result.Favorites[0].Type);
        Assert.Equal(5, result.Favorites[0].EntityId);
    }
}

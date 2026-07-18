using Application.Abstractions;
using Application.Engagement.Commands.AddFavorite;
using Domain.Engagement;
using Moq;

namespace Application.Tests;

public class AddFavoriteCommandHandlerTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AddFavoriteCommandHandler CreateHandler() => new(_favoriteRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NewFavorite_CreatesIt()
    {
        _favoriteRepository
            .Setup(r => r.GetAsync("user-1", FavoriteType.Product, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);
        _favoriteRepository.Setup(r => r.Add(It.IsAny<Favorite>())).Callback<Favorite>(f => f.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddFavoriteCommand("user-1", FavoriteType.Product, 5), CancellationToken.None);

        Assert.Equal(1, result.FavoriteId);
        _favoriteRepository.Verify(r => r.Add(It.IsAny<Favorite>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyFavorited_ReturnsExistingIdWithoutDuplicating()
    {
        var existing = new Favorite { UserId = "user-1", Type = FavoriteType.Product, EntityId = 5, CreatedAt = DateTimeOffset.UtcNow };
        existing.Id = 9;
        _favoriteRepository
            .Setup(r => r.GetAsync("user-1", FavoriteType.Product, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddFavoriteCommand("user-1", FavoriteType.Product, 5), CancellationToken.None);

        Assert.Equal(9, result.FavoriteId);
        _favoriteRepository.Verify(r => r.Add(It.IsAny<Favorite>()), Times.Never);
    }
}

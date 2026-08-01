using Application.Abstractions;
using Application.Engagement.Commands.AddFavorite;
using Domain.Engagement;
using Moq;

namespace Application.Tests;

public class AddFavoriteCommandHandlerTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AddFavoriteCommandHandler CreateHandler() =>
        new(_favoriteRepository.Object, _productRepository.Object, _storeRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_NewFavorite_CreatesIt()
    {
        _favoriteRepository
            .Setup(r => r.GetAsync("user-1", FavoriteType.Product, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);
        _productRepository.Setup(r => r.ExistsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _favoriteRepository.Setup(r => r.Add(It.IsAny<Favorite>())).Callback<Favorite>(f => f.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddFavoriteCommand("user-1", FavoriteType.Product, 5), CancellationToken.None);

        Assert.Equal(AddFavoriteOutcome.Added, result.Outcome);
        Assert.Equal(1, result.FavoriteId);
        _favoriteRepository.Verify(r => r.Add(It.IsAny<Favorite>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EntityDoesNotExist_ReturnsEntityNotFound()
    {
        _favoriteRepository
            .Setup(r => r.GetAsync("user-1", FavoriteType.Product, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);
        _productRepository.Setup(r => r.ExistsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddFavoriteCommand("user-1", FavoriteType.Product, 5), CancellationToken.None);

        Assert.Equal(AddFavoriteOutcome.EntityNotFound, result.Outcome);
        _favoriteRepository.Verify(r => r.Add(It.IsAny<Favorite>()), Times.Never);
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

        Assert.Equal(AddFavoriteOutcome.Added, result.Outcome);
        Assert.Equal(9, result.FavoriteId);
        _favoriteRepository.Verify(r => r.Add(It.IsAny<Favorite>()), Times.Never);
    }
}

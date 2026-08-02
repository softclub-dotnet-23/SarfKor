using Application.Abstractions;
using Application.Common;
using Domain.Engagement;

namespace Application.Engagement.Commands.AddFavorite;

public sealed class AddFavoriteCommandHandler(
    IFavoriteRepository favoriteRepository,
    IProductRepository productRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<AddFavoriteCommand, AddFavoriteResult>
{
    public async Task<AddFavoriteResult> Handle(AddFavoriteCommand command, CancellationToken cancellationToken)
    {
        var existing = await favoriteRepository.GetAsync(command.UserId, command.Type, command.EntityId, cancellationToken);
        if (existing is not null)
            return new AddFavoriteResult(AddFavoriteOutcome.Added, existing.Id);

        var entityExists = command.Type switch
        {
            FavoriteType.Product => await productRepository.ExistsAsync(command.EntityId, cancellationToken),
            FavoriteType.Store => await storeRepository.ExistsAsync(command.EntityId, cancellationToken),
            _ => false
        };
        if (!entityExists)
            return new AddFavoriteResult(AddFavoriteOutcome.EntityNotFound, null);

        var favorite = new Favorite
        {
            UserId = command.UserId,
            Type = command.Type,
            EntityId = command.EntityId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        favoriteRepository.Add(favorite);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddFavoriteResult(AddFavoriteOutcome.Added, favorite.Id);
    }
}

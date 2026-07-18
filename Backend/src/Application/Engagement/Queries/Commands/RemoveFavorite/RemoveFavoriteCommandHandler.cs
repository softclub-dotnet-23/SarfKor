using Application.Abstractions;
using Application.Common;

namespace Application.Engagement.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandHandler(
    IFavoriteRepository favoriteRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveFavoriteCommand, RemoveFavoriteResult>
{
    public async Task<RemoveFavoriteResult> Handle(RemoveFavoriteCommand command, CancellationToken cancellationToken)
    {
        var favorite = await favoriteRepository.GetAsync(command.UserId, command.Type, command.EntityId, cancellationToken);
        if (favorite is null)
            return new RemoveFavoriteResult(RemoveFavoriteOutcome.NotFound);

        favoriteRepository.Remove(favorite);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RemoveFavoriteResult(RemoveFavoriteOutcome.Removed);
    }
}

namespace Application.Engagement.Commands.AddFavorite;

public enum AddFavoriteOutcome
{
    Added,
    EntityNotFound
}

public sealed record AddFavoriteResult(AddFavoriteOutcome Outcome, int? FavoriteId);

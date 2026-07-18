namespace Application.Engagement.Commands.RemoveFavorite;

public enum RemoveFavoriteOutcome
{
    Removed,
    NotFound
}

public sealed record RemoveFavoriteResult(RemoveFavoriteOutcome Outcome);

using Domain.Engagement;

namespace Application.Engagement.Commands.RemoveFavorite;

public sealed record RemoveFavoriteCommand(string UserId, FavoriteType Type, int EntityId);

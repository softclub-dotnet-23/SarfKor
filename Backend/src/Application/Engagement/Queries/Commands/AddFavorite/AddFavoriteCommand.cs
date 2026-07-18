using Domain.Engagement;

namespace Application.Engagement.Commands.AddFavorite;

public sealed record AddFavoriteCommand(string UserId, FavoriteType Type, int EntityId);

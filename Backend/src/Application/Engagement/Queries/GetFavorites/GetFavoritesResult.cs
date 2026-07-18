using Domain.Engagement;

namespace Application.Engagement.Queries.GetFavorites;

public sealed record FavoriteDto(int FavoriteId, FavoriteType Type, int EntityId);

public sealed record GetFavoritesResult(IReadOnlyList<FavoriteDto> Favorites);

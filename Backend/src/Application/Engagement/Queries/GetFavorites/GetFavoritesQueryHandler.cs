using Application.Abstractions;
using Application.Common;

namespace Application.Engagement.Queries.GetFavorites;

public sealed class GetFavoritesQueryHandler(IFavoriteRepository favoriteRepository) : IQueryHandler<GetFavoritesQuery, GetFavoritesResult>
{
    public async Task<GetFavoritesResult> Handle(GetFavoritesQuery query, CancellationToken cancellationToken)
    {
        var favorites = await favoriteRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return new GetFavoritesResult(favorites.Select(f => new FavoriteDto(f.Id, f.Type, f.EntityId)).ToList());
    }
}

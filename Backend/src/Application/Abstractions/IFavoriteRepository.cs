using Domain.Engagement;

namespace Application.Abstractions;

public interface IFavoriteRepository
{
    Task<Favorite?> GetAsync(string userId, FavoriteType type, int entityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Favorite>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(Favorite favorite);
    void Remove(Favorite favorite);
}

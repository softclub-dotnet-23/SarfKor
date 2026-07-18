using Application.Abstractions;
using Domain.Engagement;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class FavoriteRepository(AppDbContext dbContext) : IFavoriteRepository
{
    public Task<Favorite?> GetAsync(string userId, FavoriteType type, int entityId, CancellationToken cancellationToken) =>
        dbContext.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.Type == type && f.EntityId == entityId, cancellationToken);

    public async Task<IReadOnlyList<Favorite>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.Favorites.Where(f => f.UserId == userId).ToListAsync(cancellationToken);

    public void Add(Favorite favorite) => dbContext.Favorites.Add(favorite);

    public void Remove(Favorite favorite) => dbContext.Favorites.Remove(favorite);
}

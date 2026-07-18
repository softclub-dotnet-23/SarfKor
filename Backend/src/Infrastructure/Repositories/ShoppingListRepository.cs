using Application.Abstractions;
using Domain.ShoppingLists;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ShoppingListRepository(AppDbContext dbContext) : IShoppingListRepository
{
    public Task<ShoppingList?> GetByIdAsync(int shoppingListId, CancellationToken cancellationToken) =>
        dbContext.ShoppingLists.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == shoppingListId, cancellationToken);

    public async Task<IReadOnlyList<ShoppingList>> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.ShoppingLists.Include(l => l.Items).Where(l => l.UserId == userId).ToListAsync(cancellationToken);

    public void Add(ShoppingList shoppingList) => dbContext.ShoppingLists.Add(shoppingList);
}

using Domain.ShoppingLists;

namespace Application.Abstractions;

public interface IShoppingListRepository
{
    Task<ShoppingList?> GetByIdAsync(int shoppingListId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShoppingList>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    void Add(ShoppingList shoppingList);
}

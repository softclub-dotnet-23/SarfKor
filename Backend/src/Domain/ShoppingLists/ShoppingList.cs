using Domain.Common;

namespace Domain.ShoppingLists;

public class ShoppingList : Entity
{
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ShoppingListItem> Items { get; set; } = [];
}

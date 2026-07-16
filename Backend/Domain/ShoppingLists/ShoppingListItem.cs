using Domain.Common;

namespace Domain.ShoppingLists;

public class ShoppingListItem : Entity
{
    public int ShoppingListId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

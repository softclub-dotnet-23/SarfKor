using Domain.Common;

namespace Domain.Inventory;

public class StockLevel : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int Quantity { get; set; }
}

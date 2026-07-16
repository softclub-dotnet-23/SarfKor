using Domain.Common;

namespace Domain.Inventory;

public class ReorderRule : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int ThresholdQuantity { get; set; }
    public int ReorderQuantity { get; set; }
    public int? PreferredSupplierId { get; set; }
    public bool IsActive { get; set; }
}

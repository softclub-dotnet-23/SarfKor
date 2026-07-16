using Domain.Common;

namespace Domain.Catalog;

public class ProductBundleItem : Entity
{
    public int ProductBundleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

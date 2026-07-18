using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Catalog;

public class ProductBundle : Entity
{
    public int StoreId { get; set; }
    public required string Name { get; set; }
    public required Money BundlePrice { get; set; }
    public List<ProductBundleItem> Items { get; set; } = [];
}

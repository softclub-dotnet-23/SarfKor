using Domain.Common;

namespace Domain.Catalog;

public class Category : Entity
{
    public required string Name { get; set; }
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsHidden { get; set; }
}

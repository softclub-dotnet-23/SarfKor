using Domain.Common;

namespace Domain.Catalog;

public class Category : Entity
{
    public required string Name { get; set; }
    public int? ParentCategoryId { get; set; }
}

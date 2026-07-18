using Domain.Common;

namespace Domain.Catalog;

public class ProductImage : Entity
{
    public int ProductId { get; set; }
    public required string ImageReference { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

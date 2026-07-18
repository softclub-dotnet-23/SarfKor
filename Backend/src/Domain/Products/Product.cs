using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Products;

public class Product : Entity
{
    public required Barcode Barcode { get; set; }
    public required string Name { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public required string CountryOfOrigin { get; set; }
    public int? TaxRateId { get; set; }
    public bool IsSoldByWeight { get; set; }
    public string? UnitOfMeasure { get; set; }
}

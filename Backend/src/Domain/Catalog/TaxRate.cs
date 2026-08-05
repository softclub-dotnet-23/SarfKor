using Domain.Common;

namespace Domain.Catalog;

public class TaxRate : Entity
{
    public required string Name { get; set; }
    public decimal Percentage { get; set; }
    public int? CategoryId { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

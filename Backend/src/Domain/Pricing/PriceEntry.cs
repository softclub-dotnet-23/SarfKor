using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Pricing;

public class PriceEntry : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public required Money Price { get; set; }
    public string? SubmittedByUserId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

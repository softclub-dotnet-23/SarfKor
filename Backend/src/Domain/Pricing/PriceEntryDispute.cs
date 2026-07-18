using Domain.Common;

namespace Domain.Pricing;

public class PriceEntryDispute : Entity
{
    public int PriceEntryId { get; set; }
    public required string DisputedByUserId { get; set; }
    public required string Reason { get; set; }
    public PriceEntryDisputeStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

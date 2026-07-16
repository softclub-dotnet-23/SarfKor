using Domain.Common;

namespace Domain.Feedback;

public class ReportDispute : Entity
{
    public int ReportId { get; set; }
    public required string DisputedByUserId { get; set; }
    public required string Reason { get; set; }
    public ReportDisputeStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

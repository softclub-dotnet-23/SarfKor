using Domain.Common;

namespace Domain.Feedback;

public class Report : Entity
{
    public required string UserId { get; set; }
    public int ProductId { get; set; }
    public int? StoreId { get; set; }
    public ReportType Type { get; set; }
    public required string Description { get; set; }
    public ReportStatus Status { get; set; }
    public string? ResolvedByAdminUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

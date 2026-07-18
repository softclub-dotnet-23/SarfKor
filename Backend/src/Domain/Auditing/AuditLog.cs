using Domain.Common;

namespace Domain.Auditing;

public class AuditLog : Entity
{
    public required string PerformedByUserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public int EntityId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

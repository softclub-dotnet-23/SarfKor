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

    // All four added for ADMIN_PROMPT.md §2.7 ("причина, IP, было → стало") — nullable so every
    // pre-existing call site (moderation-era actions, store approval, etc.) keeps compiling
    // unchanged; new Admin-operator actions from §2 populate all four.
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public string? BeforeStateJson { get; set; }
    public string? AfterStateJson { get; set; }
}

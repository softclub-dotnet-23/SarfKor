using Domain.Common;

namespace Domain.Security;

public class SecurityEvent : Entity
{
    public required string UserId { get; set; }
    public SecurityEventType Type { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

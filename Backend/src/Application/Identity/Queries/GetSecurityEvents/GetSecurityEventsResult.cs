using Domain.Security;

namespace Application.Identity.Queries.GetSecurityEvents;

public sealed record SecurityEventDto(SecurityEventType Type, string? IpAddress, string? UserAgent, DateTimeOffset OccurredAt);

public sealed record GetSecurityEventsResult(IReadOnlyList<SecurityEventDto> Events);

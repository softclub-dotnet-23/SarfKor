using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Queries.GetSecurityEvents;

public sealed class GetSecurityEventsQueryHandler(ISecurityEventRepository securityEventRepository) : IQueryHandler<GetSecurityEventsQuery, GetSecurityEventsResult>
{
    public async Task<GetSecurityEventsResult> Handle(GetSecurityEventsQuery query, CancellationToken cancellationToken)
    {
        var events = await securityEventRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var dtos = events.Select(e => new SecurityEventDto(e.Type, e.IpAddress, e.UserAgent, e.OccurredAt)).ToList();
        return new GetSecurityEventsResult(dtos);
    }
}

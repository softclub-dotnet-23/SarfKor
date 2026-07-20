using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Queries.GetUserConsents;

public sealed class GetUserConsentsQueryHandler(IUserConsentRepository userConsentRepository) : IQueryHandler<GetUserConsentsQuery, GetUserConsentsResult>
{
    public async Task<GetUserConsentsResult> Handle(GetUserConsentsQuery query, CancellationToken cancellationToken)
    {
        var consents = await userConsentRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var dtos = consents.Select(c => new UserConsentDto(c.Type, c.IsGranted, c.RecordedAt)).ToList();
        return new GetUserConsentsResult(dtos);
    }
}

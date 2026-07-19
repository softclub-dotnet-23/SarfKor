using Application.Abstractions;
using Application.Common;

namespace Application.Identity.Queries.GetUserProfile;

public sealed class GetUserProfileQueryHandler(IUserProfileRepository userProfileRepository) : IQueryHandler<GetUserProfileQuery, GetUserProfileResult>
{
    public async Task<GetUserProfileResult> Handle(GetUserProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return profile is null
            ? new GetUserProfileResult(false, null, null, null)
            : new GetUserProfileResult(true, profile.DisplayName, profile.AvatarReference, profile.PreferredLanguage);
    }
}

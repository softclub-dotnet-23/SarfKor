using Application.Abstractions;
using Application.Common;
using Domain.Identity;

namespace Application.Identity.Commands.UpdateUserProfile;

public sealed class UpdateUserProfileCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResult>
{
    public async Task<UpdateUserProfileResult> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = command.UserId,
                DisplayName = command.DisplayName,
                AvatarReference = command.AvatarReference,
                PreferredLanguage = command.PreferredLanguage
            };
            userProfileRepository.Add(profile);
        }
        else
        {
            profile.DisplayName = command.DisplayName;
            profile.AvatarReference = command.AvatarReference;
            profile.PreferredLanguage = command.PreferredLanguage;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateUserProfileResult(profile.Id);
    }
}

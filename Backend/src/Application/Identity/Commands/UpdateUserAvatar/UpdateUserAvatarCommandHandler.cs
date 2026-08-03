using Application.Abstractions;
using Application.Common;
using Domain.Identity;

namespace Application.Identity.Commands.UpdateUserAvatar;

public sealed class UpdateUserAvatarCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateUserAvatarCommand, UpdateUserAvatarResult>
{
    public async Task<UpdateUserAvatarResult> Handle(UpdateUserAvatarCommand command, CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var previous = profile?.AvatarReference;

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = command.UserId,
                DisplayName = string.Empty,
                AvatarReference = command.AvatarReference,
                PreferredLanguage = "tg"
            };
            userProfileRepository.Add(profile);
        }
        else
        {
            profile.AvatarReference = command.AvatarReference;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateUserAvatarResult(profile.Id, previous);
    }
}

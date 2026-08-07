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
                DisplayName = command.DisplayName ?? string.Empty,
                AvatarReference = command.AvatarReference,
                PreferredLanguage = command.PreferredLanguage ?? "ru"
            };
            userProfileRepository.Add(profile);
        }
        else
        {
            if (command.DisplayName is not null) profile.DisplayName = command.DisplayName;
            if (command.AvatarReference is not null) profile.AvatarReference = command.AvatarReference;
            if (command.PreferredLanguage is not null) profile.PreferredLanguage = command.PreferredLanguage;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateUserProfileResult(profile.Id);
    }
}

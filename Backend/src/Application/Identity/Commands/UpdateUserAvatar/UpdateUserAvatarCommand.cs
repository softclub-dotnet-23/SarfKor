namespace Application.Identity.Commands.UpdateUserAvatar;

public sealed record UpdateUserAvatarCommand(string UserId, string AvatarReference);

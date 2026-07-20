namespace Application.Identity.Commands.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(string UserId, string DisplayName, string? AvatarReference, string PreferredLanguage);

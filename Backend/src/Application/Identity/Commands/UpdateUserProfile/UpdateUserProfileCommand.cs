namespace Application.Identity.Commands.UpdateUserProfile;

// Null fields are preserved (not overwritten) — allows partial updates such as avatar-only.
public sealed record UpdateUserProfileCommand(string UserId, string? DisplayName, string? AvatarReference, string? PreferredLanguage);

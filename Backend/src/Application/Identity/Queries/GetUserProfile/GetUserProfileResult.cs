namespace Application.Identity.Queries.GetUserProfile;

public sealed record GetUserProfileResult(bool Found, string? DisplayName, string? AvatarReference, string? PreferredLanguage);

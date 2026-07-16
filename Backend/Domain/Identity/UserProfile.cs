using Domain.Common;

namespace Domain.Identity;

public class UserProfile : Entity
{
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarReference { get; set; }
    public string PreferredLanguage { get; set; } = "tg";
}

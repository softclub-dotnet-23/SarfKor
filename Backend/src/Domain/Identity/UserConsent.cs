using Domain.Common;

namespace Domain.Identity;

public class UserConsent : Entity
{
    public required string UserId { get; set; }
    public ConsentType Type { get; set; }
    public bool IsGranted { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

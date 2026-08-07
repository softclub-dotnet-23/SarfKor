using Domain.Common;

namespace Domain.Identity;

// Same shape/lifecycle as Domain.Stores.StoreOwnerInvitation — a second Admin account can only ever
// come from an existing Admin inviting one (ADMIN_PROMPT.md §2.7), never a public endpoint, so
// AuditLog entries always trace back to a real person instead of "an admin."
public class AdminInvitation : Entity
{
    public required string Email { get; set; }
    public required string CodeHash { get; set; }
    public int AttemptCount { get; set; }
    public required string InvitedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

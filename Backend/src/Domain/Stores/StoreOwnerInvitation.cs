using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Stores;

// Holds the pending store's details until the invited owner confirms their email with the 6-digit
// code — Store.OwnerUserId is a non-nullable required field, so the real Store row can't exist yet
// (no account exists for the invitee at invite time).
public class StoreOwnerInvitation : Entity
{
    public required string Email { get; set; }
    public required string StoreName { get; set; }
    public required string Address { get; set; }
    public required GeoLocation Location { get; set; }
    public required string CodeHash { get; set; }
    public int AttemptCount { get; set; }
    public required string InvitedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

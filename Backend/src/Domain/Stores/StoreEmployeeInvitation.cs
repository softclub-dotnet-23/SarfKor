using Domain.Common;

namespace Domain.Stores;

public enum StoreEmployeeInvitationStatus
{
    Pending,
    Accepted,
    Revoked,
    Expired
}

public class StoreEmployeeInvitation : Entity
{
    public int StoreId { get; set; }
    public required string Email { get; set; }
    public StoreEmployeeRole Role { get; set; }

    /// <summary>Only the hash is ever persisted — see Application.Common.InviteToken. The raw
    /// token exists only in memory long enough to email it, and in the accept link itself.</summary>
    public required string TokenHash { get; set; }

    public required string InvitedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public StoreEmployeeInvitationStatus Status { get; set; } = StoreEmployeeInvitationStatus.Pending;

    /// <summary>Set once Status is Accepted — the user that ended up attached to the store, whether
    /// brand-new or a pre-existing account the invite just linked up.</summary>
    public string? AcceptedUserId { get; set; }

    /// <summary>Bumped on every send/resend — lets a resend refresh ExpiresAt/rotate the token
    /// without losing when the invitation was first created.</summary>
    public DateTimeOffset LastSentAt { get; set; }

    public bool IsEffectivelyExpired(DateTimeOffset now) => Status == StoreEmployeeInvitationStatus.Pending && ExpiresAt < now;
}

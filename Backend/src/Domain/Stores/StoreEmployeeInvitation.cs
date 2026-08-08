using Domain.Common;

namespace Domain.Stores;

public enum StoreEmployeeInvitationStatus
{
    Pending,
    Accepted,
    Revoked,
    Expired
}

/// <summary>One invitation mechanism for the whole platform, not one per role — originally just a
/// cashier/owner invite (hence the name, kept to avoid a bigger rename+migration than the
/// generalization needs), now also backs /admin/users' "Добавить пользователя" (any Identity role:
/// User/StorePartner/Admin). StoreId/Role are only set when InvitedRole is StorePartner — a plain
/// User or Admin invite grants no store membership, just the Identity role named by InvitedRole
/// once accepted (see AcceptStoreEmployeeInvitationCommandHandler).</summary>
public class StoreEmployeeInvitation : Entity
{
    /// <summary>Null for a platform-wide invite (InvitedRole User/Admin) — set iff InvitedRole is
    /// StorePartner, naming which store the invitee is being attached to.</summary>
    public int? StoreId { get; set; }

    public required string Email { get; set; }

    /// <summary>Owner/Cashier sub-role within StoreId — null unless InvitedRole is StorePartner.</summary>
    public StoreEmployeeRole? Role { get; set; }

    /// <summary>The ASP.NET Identity role granted on acceptance: "User" | "StorePartner" | "Admin".
    /// Defaults to StorePartner for the original store-employee-invite call sites, which never set
    /// it explicitly before this field existed.</summary>
    public string InvitedRole { get; set; } = "StorePartner";

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

namespace Application.Stores;

/// <summary>Bound from the "StoreEmployeeInvitations" config section — same pattern as
/// Application.Subscriptions.SubscriptionOptions.</summary>
public sealed class StoreEmployeeInvitationOptions
{
    public const string SectionName = "StoreEmployeeInvitations";

    /// <summary>How long an invite link stays valid after it's (re)sent.</summary>
    public int ExpiryDays { get; set; } = 7;
}

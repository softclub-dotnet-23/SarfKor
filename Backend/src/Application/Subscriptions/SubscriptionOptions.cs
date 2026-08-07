namespace Application.Subscriptions;

/// <summary>Bound from the "Subscriptions" config section (appsettings.json / user-secrets / env
/// vars) — same pattern as Application.Assistant.AssistantOptions.</summary>
public sealed class SubscriptionOptions
{
    public const string SectionName = "Subscriptions";

    /// <summary>ADMIN_PROMPT.md §2.1: "пробный период на N дней (N — настройка, по умолчанию 14)".</summary>
    public int TrialDurationDays { get; set; } = 14;

    /// <summary>ADMIN_PROMPT.md §2.1: "PastDue дольше льготного периода (настройка, по умолчанию
    /// 7 дней) → Suspended".</summary>
    public int PastDueGracePeriodDays { get; set; } = 7;

    /// <summary>Which SubscriptionPlan.Code a newly approved store's Trial subscription is issued
    /// against — null falls back to the cheapest active plan (ApproveStoreCommandHandler).</summary>
    public string? DefaultPlanCode { get; set; }
}

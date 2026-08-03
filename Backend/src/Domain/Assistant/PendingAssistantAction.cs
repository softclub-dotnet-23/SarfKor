using Domain.Common;

namespace Domain.Assistant;

/// <summary>
/// A Mode C proposal the assistant has drafted but not executed yet. Created when a "Propose*" tool
/// runs during chat; only turned into a real mutation by <c>ConfirmAssistantActionCommand</c>, which
/// is a separate, explicitly user-confirmed request (never triggered by the chat turn itself).
/// <see cref="ConfirmedAt"/> makes confirming idempotent: a retried confirm request for an
/// already-confirmed action returns the same success without executing twice.
/// </summary>
public class PendingAssistantAction : Entity
{
    public required string RequestedByUserId { get; set; }
    public int StoreId { get; set; }
    public AssistantActionType ActionType { get; set; }

    /// <summary>Serialized (JSON) arguments captured at propose time — never re-derived from a later, possibly different chat message.</summary>
    public required string ParametersJson { get; set; }

    /// <summary>Human-readable description shown to the user before they confirm ("Set price of X to Y TJS").</summary>
    public required string Summary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

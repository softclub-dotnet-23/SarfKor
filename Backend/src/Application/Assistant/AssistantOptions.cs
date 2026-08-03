namespace Application.Assistant;

/// <summary>Bound from the "Assistant" config section (appsettings.json / user-secrets / env vars).</summary>
public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>Mode C (propose-and-confirm mutations) is fully implemented but off by default —
    /// ConfirmAssistantActionCommandHandler re-checks this at confirm time too, not just when
    /// deciding whether to register the Propose* tools, so flipping it off mid-session can't be
    /// raced past by an already-open chat.</summary>
    public bool ActionsEnabled { get; set; }

    /// <summary>Caps the tool-call round-trip loop in AskAssistantCommandHandler — a runaway
    /// "call a tool, get a result, call another tool, ..." loop must terminate, not just rely on
    /// the model to stop on its own.</summary>
    public int MaxToolIterations { get; set; } = 6;

    /// <summary>Client-supplied history is stateless (see AskAssistantCommand) — this bounds how much
    /// of it is forwarded to the model per request, both for cost and for context-length safety.</summary>
    public int MaxHistoryMessages { get; set; } = 20;

    public int MaxMessageLength { get; set; } = 2000;

    public int PendingActionLifetimeMinutes { get; set; } = 15;
}

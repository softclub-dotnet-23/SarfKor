namespace Domain.Assistant;

/// <summary>
/// The set of mutating actions the assistant is allowed to *propose* (Mode C). Each member maps to
/// exactly one existing Command handler in Application — the assistant never invents a new mutation
/// path, it only ever asks for confirmation before calling one that already exists and is already
/// authorized/audited on its own.
/// </summary>
public enum AssistantActionType
{
    SetPrice,
    RecordStockReceipt,
    CreatePromotion,
}

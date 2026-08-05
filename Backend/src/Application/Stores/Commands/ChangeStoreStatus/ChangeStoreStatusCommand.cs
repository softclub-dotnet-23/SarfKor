using Domain.Stores;

namespace Application.Stores.Commands.ChangeStoreStatus;

/// <summary>
/// One handler for every administrative Store.Status transition except Approve (ApproveStoreCommand
/// stays separate — it also issues a Trial subscription, a side effect none of these share).
/// Reason is required for every transition here per ADMIN_PROMPT.md §2 ("каждая операция,
/// отключающая кого-либо, обязательно требует причину") — Reject/Suspend/Block/Archive all close
/// access, and even the "re-open" transitions (Unsuspend/Unblock) record why for the audit trail.
/// </summary>
public sealed record ChangeStoreStatusCommand(int StoreId, StoreStatus NewStatus, string Reason, string PerformedByUserId, string? PerformedByIpAddress = null);

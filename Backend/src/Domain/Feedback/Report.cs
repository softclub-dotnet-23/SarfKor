using Domain.Common;

namespace Domain.Feedback;

// Purely a signal now, not a queue: no admin ever manually resolves a Report (see
// ADMIN_PROMPT.md §1 — moderation removed). It still gets created (ReportOutOfStockCommand) and
// still matters — accumulated reports against the same user/store automatically move
// ContributorTrustScore and the "problem stores" metric (§2.4/§2.5) — it just never carries a
// manual-review outcome anymore.
public class Report : Entity
{
    public required string UserId { get; set; }
    public int ProductId { get; set; }
    public int? StoreId { get; set; }
    public ReportType Type { get; set; }
    public required string Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

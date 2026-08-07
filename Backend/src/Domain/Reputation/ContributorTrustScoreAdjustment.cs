using Domain.Common;

namespace Domain.Reputation;

// Every change to a ContributorTrustScore.Score — automatic (a submitted price got confirmed/
// refuted, a report landed against the user) or manual (Admin correction) — is appended here first.
// Score itself is always the running sum, so IsManual rows are never at risk of being overwritten
// by the next automatic recalculation (ADMIN_PROMPT.md §2.4: there is no "recalculation", only
// new additive events).
public class ContributorTrustScoreAdjustment : Entity
{
    public required string UserId { get; set; }
    public double Delta { get; set; }
    public required string Reason { get; set; }
    public bool IsManual { get; set; }
    public string? PerformedByAdminUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

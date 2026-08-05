using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Pricing;

public class PriceEntry : Entity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public required Money Price { get; set; }
    public string? SubmittedByUserId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    // Replaces the old manual PriceEntryDispute queue (ADMIN_PROMPT.md §1): a submission from a
    // low-ContributorTrustScore author starts unverified and is excluded from public results
    // (ScanBarcodeQueryHandler) until a second, independent submission corroborates it — see
    // TrustScoreFormula and SubmitPriceUpdateCommandHandler. Always true for a trusted author.
    public bool IsVerified { get; set; } = true;
}

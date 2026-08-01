using Domain.Common;

namespace Domain.Payments;

/// <summary>
/// Append-only ledger of gift card redemptions (mirrors StockMovement/LoyaltyTransaction) — the
/// audit trail GiftCard itself never had. Redemption is intentionally allowed at any store
/// regardless of IssuingStoreId; StoreId here is what makes that reconcilable after the fact.
/// </summary>
public class GiftCardRedemption : Entity
{
    public int GiftCardId { get; set; }
    public int StoreId { get; set; }
    public required decimal Amount { get; set; }
    public int? SaleTransactionId { get; set; }
    public DateTimeOffset RedeemedAt { get; set; }
}

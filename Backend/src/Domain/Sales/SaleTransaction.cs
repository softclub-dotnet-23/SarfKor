using Domain.Common;

namespace Domain.Sales;

public class SaleTransaction : Entity
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public required string CashierUserId { get; set; }
    public required string IdempotencyKey { get; set; }
    public SaleStatus Status { get; set; }
    public required string Currency { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }

    // Persisted so VoidSaleCommandHandler can refund what was actually applied — without these,
    // voiding a sale that used a gift card or store credit silently lost that value forever.
    public int? GiftCardId { get; set; }
    public decimal? GiftCardAmountApplied { get; set; }
    public decimal? StoreCreditAmountApplied { get; set; }

    public List<SaleLineItem> Lines { get; set; } = [];
}
